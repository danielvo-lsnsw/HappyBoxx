export interface InvoiceLineInput {
  quantity: number | string;
  unitPriceInclusive: number | string;
}

export function calculateInvoiceTotals(lines: InvoiceLineInput[]) {
  const total = lines.reduce((sum, line) => {
    const amount = Number(line.quantity || 0) * Number(line.unitPriceInclusive || 0);
    return sum + Math.round((amount + Number.EPSILON) * 100) / 100;
  }, 0);
  const roundedTotal = Math.round((total + Number.EPSILON) * 100) / 100;
  const gstAmount = Math.round((roundedTotal / 11 + Number.EPSILON) * 100) / 100;
  const subtotal = Math.round((roundedTotal - gstAmount + Number.EPSILON) * 100) / 100;

  return { subtotal, gstAmount, total: roundedTotal };
}
