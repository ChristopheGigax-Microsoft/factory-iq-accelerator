type ErrorBannerProps = {
  title?: string;
  message: string;
};

export function ErrorBanner({ title = 'Action required', message }: ErrorBannerProps) {
  return (
    <div className="animate-fade-in-up rounded-xl border border-amber-200/60 bg-gradient-to-r from-amber-50 to-orange-50 p-4 shadow-sm">
      <div className="flex items-start gap-3">
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-amber-100">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="text-amber-600">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
            <line x1="12" y1="9" x2="12" y2="13" />
            <line x1="12" y1="17" x2="12.01" y2="17" />
          </svg>
        </div>
        <div>
          <p className="text-sm font-semibold text-amber-900">{title}</p>
          <p className="mt-0.5 text-sm text-amber-700">{message}</p>
        </div>
      </div>
    </div>
  );
}
