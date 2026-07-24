import { createContext, useContext } from 'react';

export interface ToastApi {
  /** Surface a transient message. Showing the same text again restarts its countdown. */
  show: (message: string) => void;
}

export const ToastContext = createContext<ToastApi | null>(null);

export function useToast(): ToastApi {
  const api = useContext(ToastContext);
  if (api === null) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return api;
}
