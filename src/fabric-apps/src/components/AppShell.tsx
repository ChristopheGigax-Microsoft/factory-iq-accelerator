import { Link, useLocation } from 'react-router-dom';

import { useAuth } from '@/hooks/AuthContext';

const navItems = [
  { path: '/', label: 'Dashboard', icon: DashboardIcon },
  { path: '/baseline', label: 'Baseline Manager', icon: HierarchyIcon },
  { path: '/graph', label: 'Hierarchy Graph', icon: GraphIcon },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const { signOut, user } = useAuth();
  const location = useLocation();

  return (
    <div className="flex h-screen overflow-hidden bg-gray-50/80 gradient-mesh">
      {/* Sidebar */}
      <aside className="flex w-64 flex-col border-r border-gray-200/60 bg-white/60 backdrop-blur-xl">
        {/* Brand */}
        <div className="flex items-center gap-3 px-6 py-5 border-b border-gray-100">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-blue-600 to-indigo-600 shadow-md shadow-blue-600/20">
            <FactoryIcon />
          </div>
          <div>
            <h1 className="text-sm font-bold text-gray-900 tracking-tight">Factory IQ</h1>
            <p className="text-[10px] font-medium text-gray-400 uppercase tracking-widest">ISA-95 Manager</p>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-3 py-4 space-y-1">
          {navItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200 ${
                  isActive
                    ? 'bg-gradient-to-r from-blue-50 to-indigo-50 text-blue-700 shadow-sm'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                }`}
              >
                <span
                  className={`flex h-8 w-8 items-center justify-center rounded-lg transition-all duration-200 ${
                    isActive
                      ? 'bg-white shadow-sm text-blue-600'
                      : 'text-gray-400 group-hover:text-gray-600'
                  }`}
                >
                  <item.icon />
                </span>
                {item.label}
                {isActive && (
                  <span className="ml-auto h-1.5 w-1.5 rounded-full bg-blue-500 animate-pulse-ring" />
                )}
              </Link>
            );
          })}
        </nav>

        {/* User */}
        <div className="border-t border-gray-100 px-4 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-emerald-400 to-emerald-500 text-xs font-bold text-white shadow-sm">
              {user?.name?.charAt(0)?.toUpperCase() || 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-gray-700 truncate">{user?.name || 'User'}</p>
              <p className="text-[10px] text-gray-400 truncate">{user?.id || ''}</p>
            </div>
            <button
              onClick={() => void signOut()}
              className="rounded-lg p-1.5 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-600"
              aria-label="Sign out"
              title="Sign out"
            >
              <LogoutIcon />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-6xl px-8 py-8">
          {children}
        </div>
      </main>
    </div>
  );
}

function FactoryIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-white">
      <path d="M2 20a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8l-7 5V8l-7 5V4a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z" />
    </svg>
  );
}

function DashboardIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="9" rx="1" />
      <rect x="14" y="3" width="7" height="5" rx="1" />
      <rect x="14" y="12" width="7" height="9" rx="1" />
      <rect x="3" y="16" width="7" height="5" rx="1" />
    </svg>
  );
}

function HierarchyIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="5" r="3" />
      <line x1="12" y1="8" x2="12" y2="14" />
      <line x1="6" y1="14" x2="18" y2="14" />
      <line x1="6" y1="14" x2="6" y2="17" />
      <line x1="12" y1="14" x2="12" y2="17" />
      <line x1="18" y1="14" x2="18" y2="17" />
      <circle cx="6" cy="19" r="2" />
      <circle cx="12" cy="19" r="2" />
      <circle cx="18" cy="19" r="2" />
    </svg>
  );
}

function LogoutIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </svg>
  );
}

function GraphIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="5" cy="6" r="2" />
      <circle cx="12" cy="12" r="2" />
      <circle cx="19" cy="6" r="2" />
      <circle cx="5" cy="18" r="2" />
      <circle cx="19" cy="18" r="2" />
      <line x1="6.5" y1="7.5" x2="10.5" y2="10.5" />
      <line x1="13.5" y1="10.5" x2="17.5" y2="7.5" />
      <line x1="6.5" y1="16.5" x2="10.5" y2="13.5" />
      <line x1="13.5" y1="13.5" x2="17.5" y2="16.5" />
    </svg>
  );
}
