export class ConfirmService {
  // Simple confirm wrapper returning a Promise<boolean> for async usage.
  // This currently uses window.confirm but centralizes calls so it can be replaced by a modal implementation later.
  confirm(message: string, title?: string): Promise<boolean> {
    const txt = title ? `${title}\n\n${message}` : message;
    return Promise.resolve(window.confirm(txt));
  }
}
