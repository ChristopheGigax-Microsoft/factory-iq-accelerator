type ErrorBannerProps = {
  title?: string;
  message: string;
};

export function ErrorBanner({ title = 'Action required', message }: ErrorBannerProps) {
  return (
    <div className="mb-4 rounded-md border border-amber-300 bg-amber-50 p-3 text-amber-900">
      <p className="font-semibold">{title}</p>
      <p className="text-sm">{message}</p>
    </div>
  );
}
