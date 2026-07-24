import { useCallback, useEffect, useMemo, useRef, useState, type ReactElement, type ReactNode } from 'react';
import { ToastContext } from '../state/toastContext';

const DISMISS_MS = 5000;

/**
 * Hosts the one transient message the UI can be showing at a time. It exists because a control
 * that refuses to act has to say why *at the moment it refuses* — a tooltip only reaches a DM
 * who already suspected something was wrong and went hunting for it.
 *
 * Deliberately no queue: these messages explain a click that just happened, and a backlog of
 * stale explanations is worse than dropping all but the newest.
 */
export function ToastProvider({ children }: { children: ReactNode }): ReactElement {
  const [message, setMessage] = useState<string | null>(null);
  // Clicking the same dead control twice must restart the countdown, and setting state to the
  // identical string is a no-op — so the timer keys on this instead of on the text.
  const [issued, setIssued] = useState(0);
  const timer = useRef<number | undefined>(undefined);

  const show = useCallback((next: string) => {
    setMessage(next);
    setIssued((n) => n + 1);
  }, []);

  useEffect(() => {
    if (message === null) return;
    timer.current = window.setTimeout(() => setMessage(null), DISMISS_MS);
    return () => window.clearTimeout(timer.current);
  }, [message, issued]);

  const api = useMemo(() => ({ show }), [show]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      {/* The live region stays mounted so its content is announced when it changes; mounting it
          alongside the message would race the announcement. */}
      <div
        role="status"
        aria-live="polite"
        style={{
          position: 'fixed',
          bottom: 'var(--space-lg)',
          left: '50%',
          transform: 'translateX(-50%)',
          display: 'flex',
          justifyContent: 'center',
          pointerEvents: 'none',
        }}
      >
        {message !== null && (
          <div
            style={{
              maxWidth: '32rem',
              padding: 'var(--space-sm) var(--space-md)',
              background: 'var(--surface-raised)',
              color: 'var(--ink)',
              border: '1px solid var(--border)',
              borderLeft: '4px solid var(--accent)',
              borderRadius: 'var(--radius-sm)',
              fontFamily: 'var(--font-body)',
              boxShadow: 'var(--shadow-raised)',
            }}
          >
            {message}
          </div>
        )}
      </div>
    </ToastContext.Provider>
  );
}
