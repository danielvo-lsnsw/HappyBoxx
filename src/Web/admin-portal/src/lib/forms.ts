import { notifications } from '@mantine/notifications';
import { ApiError } from './api/httpClient';

interface FormLike {
  setFieldError: (path: string, error: string) => void;
}

const toFieldPath = (serverKey: string) => serverKey.charAt(0).toLowerCase() + serverKey.slice(1);

/**
 * Maps server validation (400) and known error codes onto form fields.
 * Returns true when at least one field error was set.
 */
export function applyApiErrors(
  form: FormLike,
  error: unknown,
  errorCodeFields: Record<string, string> = {},
): boolean {
  if (!(error instanceof ApiError)) {
    return false;
  }

  let applied = false;
  for (const [key, messages] of Object.entries(error.problem.errors ?? {})) {
    if (messages[0]) {
      form.setFieldError(toFieldPath(key), messages[0]);
      applied = true;
    }
  }

  const field = error.problem.errorCode ? errorCodeFields[error.problem.errorCode] : undefined;
  if (field) {
    form.setFieldError(field, error.message);
    applied = true;
  }

  return applied;
}

export function notifySuccess(message: string, title = 'Saved') {
  notifications.show({ color: 'green', title, message });
}

export function notifyError(error: unknown, title = 'Something went wrong') {
  notifications.show({
    color: 'red',
    title,
    message: error instanceof Error ? error.message : 'Please try again.',
    autoClose: 8000,
  });
}

/** Standard submit-error handling: field errors when possible, otherwise a toast. */
export function handleSubmitError(
  form: FormLike,
  error: unknown,
  errorCodeFields?: Record<string, string>,
) {
  if (!applyApiErrors(form, error, errorCodeFields)) {
    notifyError(error);
  }
}
