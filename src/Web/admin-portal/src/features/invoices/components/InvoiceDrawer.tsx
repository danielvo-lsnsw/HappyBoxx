import {
  Alert,
  Button,
  Checkbox,
  Group,
  Modal,
  Skeleton,
  Stack,
  Table,
  TagsInput,
  Text,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { isEmail } from '@mantine/form';
import { IconMail, IconPrinter } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { formatCurrency, formatDate, formatNumber } from '@/lib/format';
import { notifyError, notifySuccess } from '@/lib/forms';
import { useEmailInvoiceMutation, useInvoiceQuery, useRecordInvoicePaymentMutation } from '../api';
import type { DuplicateInvoiceDraft, Invoice } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

interface InvoiceDrawerProps {
  invoiceId?: string;
  onClose: () => void;
  onDuplicate: (draft: DuplicateInvoiceDraft) => void;
}

function todayAsLocalDate() {
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${today.getFullYear()}-${month}-${day}`;
}

function buildDuplicateDraft(invoice: Invoice): DuplicateInvoiceDraft {
  return {
    customerId: invoice.customerId,
    lines: invoice.lines.map((line) => ({
      productId: line.productId,
      priceTier: line.priceTier,
      description: line.description,
      unit: line.unit,
      quantity: line.quantity,
      unitPriceInclusive: line.unitPriceInclusive,
    })),
  };
}

export function InvoiceDrawer({ invoiceId, onClose, onDuplicate }: InvoiceDrawerProps) {
  const { t } = useUiLanguage();
  const invoiceQuery = useInvoiceQuery(invoiceId);
  const invoice = invoiceQuery.data;
  const emailInvoice = useEmailInvoiceMutation(invoiceId ?? '');
  const recordPayment = useRecordInvoicePaymentMutation(invoiceId ?? '');
  const [emailModalOpened, emailModal] = useDisclosure(false);
  const [paymentModalOpened, paymentModal] = useDisclosure(false);
  const [recipientEmails, setRecipientEmails] = useState<string[]>([]);
  const [hasTriedSendEmail, setHasTriedSendEmail] = useState(false);
  const [includeInvoicePdf, setIncludeInvoicePdf] = useState(true);
  const [paidOn, setPaidOn] = useState(todayAsLocalDate());
  const [paymentReference, setPaymentReference] = useState('');
  const [paymentNote, setPaymentNote] = useState('');
  const [sendReceiptEmail, setSendReceiptEmail] = useState(false);
  const [includeInvoiceInReceiptEmail, setIncludeInvoiceInReceiptEmail] = useState(true);
  const [receiptEmails, setReceiptEmails] = useState<string[]>([]);
  const [hasTriedSavePayment, setHasTriedSavePayment] = useState(false);

  const recipientError = useMemo(() => {
    if (recipientEmails.length === 0) {
      return t('At least one email is required');
    }

    const invalidEmail = recipientEmails.find((email) => !isEmail(email.trim()));
    return invalidEmail ? t('Enter valid email addresses') : null;
  }, [recipientEmails, t]);
  const receiptRecipientError = useMemo(() => {
    if (!sendReceiptEmail) {
      return null;
    }

    if (receiptEmails.length === 0) {
      return t('At least one email is required');
    }

    const invalidEmail = receiptEmails.find((email) => !isEmail(email.trim()));
    return invalidEmail ? t('Enter valid email addresses') : null;
  }, [receiptEmails, sendReceiptEmail, t]);
  const isPaid = !!invoice?.paidOn;
  const emailActionLabel = isPaid ? t('Email receipt') : t('Email invoice');
  const emailSuccessTitle = isPaid ? t('Receipt email sent') : t('Invoice email sent');
  const emailErrorTitle = isPaid ? t('Could not email receipt') : t('Could not email invoice');

  const printInvoice = () => {
    document.body.classList.add('printing-invoice');
    window.print();
    document.body.classList.remove('printing-invoice');
  };

  const openEmailModal = () => {
    if (!invoice) {
      return;
    }

    const defaults = invoice.customerEmail ? [invoice.customerEmail] : [];
    setRecipientEmails(defaults);
    setIncludeInvoicePdf(true);
    setHasTriedSendEmail(false);
    emailModal.open();
  };

  const openPaymentModal = () => {
    if (!invoice) {
      return;
    }

    setPaidOn(invoice.paidOn ?? todayAsLocalDate());
    setPaymentReference(invoice.paymentReference ?? '');
    setPaymentNote(invoice.paymentNote ?? '');
    const defaults = invoice.customerEmail ? [invoice.customerEmail] : [];
    setReceiptEmails(defaults);
    setSendReceiptEmail(false);
    setIncludeInvoiceInReceiptEmail(true);
    setHasTriedSavePayment(false);
    paymentModal.open();
  };

  const sendInvoiceEmail = async () => {
    if (!invoiceId) {
      return;
    }

    setHasTriedSendEmail(true);
    if (recipientError) {
      return;
    }

    try {
      const payload = {
        toEmails: recipientEmails.map((email) => email.trim()),
        includeInvoice: isPaid ? includeInvoicePdf : false,
      };
      await emailInvoice.mutateAsync(payload);
      notifySuccess(emailSuccessTitle, t('Email sent'));
      emailModal.close();
      setHasTriedSendEmail(false);
    } catch (error) {
      notifyError(error, emailErrorTitle);
    }
  };

  const submitPayment = async () => {
    if (!invoiceId) {
      return;
    }

    setHasTriedSavePayment(true);
    if (sendReceiptEmail && receiptRecipientError) {
      return;
    }

    try {
      await recordPayment.mutateAsync({
        paidOn,
        paymentReference: paymentReference.trim() || undefined,
        paymentNote: paymentNote.trim() || undefined,
        sendReceiptEmail,
        receiptToEmails: sendReceiptEmail ? receiptEmails.map((email) => email.trim()) : undefined,
        includeInvoice: sendReceiptEmail ? includeInvoiceInReceiptEmail : false,
      });
      notifySuccess(
        sendReceiptEmail ? t('Payment saved and receipt emailed') : t('Payment saved'),
        t('Saved'),
      );
      paymentModal.close();
      setHasTriedSavePayment(false);
    } catch (error) {
      notifyError(error, t('Could not save payment'));
    }
  };

  return (
    <>
      <DetailDrawer
        opened={!!invoiceId}
        onClose={onClose}
        title={invoice ? `${t('Invoice')} ${invoice.invoiceNumber}` : t('Invoice')}
        subtitle={invoice && `${invoice.customerName} · ${formatDate(invoice.invoiceDate)}`}
        drawerSize={920}
        footer={
          invoice && (
            <>
              <Button variant="default" onClick={onClose}>
                {t('Close')}
              </Button>
              <Button
                variant="default"
                leftSection={<IconMail size={18} />}
                onClick={openEmailModal}
              >
                {emailActionLabel}
              </Button>
              <Button
                variant="default"
                onClick={() => invoice && onDuplicate(buildDuplicateDraft(invoice))}
              >
                {t('Create copy')}
              </Button>
              <Button variant="default" onClick={openPaymentModal}>
                {t('Record payment')}
              </Button>
              <Button leftSection={<IconPrinter size={18} />} onClick={printInvoice}>
                {t('Print / Save PDF')}
              </Button>
            </>
          )
        }
      >
        {invoiceQuery.isPending && <Skeleton height={400} radius="md" />}
        {invoiceQuery.isError && (
          <Alert color="red" title={t("Couldn't load invoice")}>
            {invoiceQuery.error.message}
          </Alert>
        )}
        {invoice && <InvoiceDocument invoice={invoice} />}
      </DetailDrawer>
      {invoice && <InvoiceDocument className="invoice-print-area" invoice={invoice} />}
      <Modal
        opened={emailModalOpened}
        onClose={emailModal.close}
        title={emailActionLabel}
        centered
        size={500}
      >
        <Stack>
          <Text size="sm" c="dimmed">
            {t('Add one or more email addresses. Press Enter after each email.')}
          </Text>
          {isPaid && (
            <Checkbox
              label={t('Include invoice PDF')}
              checked={includeInvoicePdf}
              onChange={(event) => setIncludeInvoicePdf(event.currentTarget.checked)}
            />
          )}
          <TagsInput
            label={t('Recipient emails')}
            placeholder={t('Type an email and press Enter')}
            value={recipientEmails}
            onChange={setRecipientEmails}
            error={hasTriedSendEmail ? recipientError : undefined}
            clearable
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={emailModal.close}>
              {t('Cancel')}
            </Button>
            <Button onClick={() => void sendInvoiceEmail()} loading={emailInvoice.isPending}>
              {t('Send email')}
            </Button>
          </Group>
        </Stack>
      </Modal>
      <Modal
        opened={paymentModalOpened}
        onClose={paymentModal.close}
        title={t('Record payment')}
        centered
        size={500}
      >
        <Stack>
          <TextInput
            label={t('Paid on')}
            type="date"
            value={paidOn}
            onChange={(event) => setPaidOn(event.currentTarget.value)}
          />
          <TextInput
            label={t('Payment reference')}
            value={paymentReference}
            onChange={(event) => setPaymentReference(event.currentTarget.value)}
          />
          <Textarea
            label={t('Payment note')}
            minRows={2}
            autosize
            value={paymentNote}
            onChange={(event) => setPaymentNote(event.currentTarget.value)}
          />
          <Checkbox
            label={t('Send receipt email')}
            checked={sendReceiptEmail}
            onChange={(event) => setSendReceiptEmail(event.currentTarget.checked)}
          />
          {sendReceiptEmail && (
            <Stack gap="xs">
              <Checkbox
                label={t('Include invoice PDF')}
                checked={includeInvoiceInReceiptEmail}
                onChange={(event) => setIncludeInvoiceInReceiptEmail(event.currentTarget.checked)}
              />
              <TagsInput
                label={t('Recipient emails')}
                placeholder={t('Type an email and press Enter')}
                value={receiptEmails}
                onChange={setReceiptEmails}
                error={hasTriedSavePayment ? receiptRecipientError : undefined}
                clearable
              />
            </Stack>
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={paymentModal.close}>
              {t('Cancel')}
            </Button>
            <Button onClick={() => void submitPayment()} loading={recordPayment.isPending}>
              {t('Save payment')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}

function InvoiceDocument({ invoice, className }: { invoice: Invoice; className?: string }) {
  const { t } = useUiLanguage();

  return (
    <Stack className={className} gap="lg">
      <Group justify="space-between" align="flex-start">
        <Stack gap={2}>
          <Text size="xl" fw={800}>
            HAPPY BOXX PTY LTD
          </Text>
          <Text size="sm">3/8 Enterprise Street, Salisbury QLD 4107</Text>
          <Text size="sm">Phone: 0466 115 997 · happyboxx.info@gmail.com</Text>
          <Text size="sm">ABN: 59 629 823 285</Text>
        </Stack>
        <Text size="xl" fw={800} tt="uppercase">
          {t('Tax invoice')}
        </Text>
      </Group>

      <Group grow align="flex-start">
        <Stack gap={2}>
          <Text size="sm" fw={700} tt="uppercase">
            {t('Bill to')}
          </Text>
          <Text fw={600}>{invoice.customerName}</Text>
          <Text size="sm" style={{ whiteSpace: 'pre-line' }}>
            {invoice.customerAddress}
          </Text>
          {invoice.customerPhone && (
            <Text size="sm">
              {t('Phone')}: {invoice.customerPhone}
            </Text>
          )}
        </Stack>
        <Stack gap={2}>
          <Text size="sm">
            <strong>{t('Status')}:</strong> {invoice.paidOn ? t('Paid') : t('Unpaid')}
          </Text>
          <Text size="sm">
            <strong>{t('Invoice number')}:</strong> {invoice.invoiceNumber}
          </Text>
          <Text size="sm">
            <strong>{t('Invoice date')}:</strong> {formatDate(invoice.invoiceDate)}
          </Text>
          {invoice.paidOn && (
            <Text size="sm">
              <strong>{t('Paid on')}:</strong> {formatDate(invoice.paidOn)}
            </Text>
          )}
          {invoice.paymentReference && (
            <Text size="sm">
              <strong>{t('Payment reference')}:</strong> {invoice.paymentReference}
            </Text>
          )}
        </Stack>
      </Group>

      <Table withTableBorder withColumnBorders>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>{t('Description')}</Table.Th>
            <Table.Th>{t('Unit')}</Table.Th>
            <Table.Th ta="right">{t('Quantity')}</Table.Th>
            <Table.Th ta="right">{t('Unit price (incl. GST)')}</Table.Th>
            <Table.Th ta="right">{t('Amount')}</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {invoice.lines.map((line) => (
            <Table.Tr key={line.id}>
              <Table.Td>{line.description}</Table.Td>
              <Table.Td>{line.unit}</Table.Td>
              <Table.Td ta="right">{formatNumber(line.quantity)}</Table.Td>
              <Table.Td ta="right">{formatCurrency(line.unitPriceInclusive)}</Table.Td>
              <Table.Td ta="right">{formatCurrency(line.amountInclusive)}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      <Stack gap={4} align="flex-end">
        <Text size="sm">
          {t('Subtotal (ex GST)')}: {formatCurrency(invoice.subtotal)}
        </Text>
        <Text size="sm">
          {t('GST included (10%)')}: {formatCurrency(invoice.gstAmount)}
        </Text>
        <Text fw={800} size="lg">
          {t('Total (incl. GST)')}: {formatCurrency(invoice.total)}
        </Text>
      </Stack>

      <Stack gap={4}>
        <Text size="sm" fw={700}>
          {t('Payment details')}
        </Text>
        <Text size="sm">ANZ · BSB 014219 · Account 470777199 · HAPPYBOXX PTY LTD</Text>
        <Text size="sm">
          {t('Please pay within 7 days and use the invoice number as reference.')}
        </Text>
      </Stack>
    </Stack>
  );
}
