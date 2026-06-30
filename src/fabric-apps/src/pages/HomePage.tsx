import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';

import { useAuth } from '@/hooks/AuthContext';
import { getBaselineHierarchy, type BaselineNode } from '@/services/baselineClient';

const nodeTypeColors: Record<string, string> = {
  Enterprise: 'from-blue-500 to-indigo-600',
  Site: 'from-emerald-400 to-emerald-600',
  Area: 'from-amber-400 to-orange-500',
  WorkCenter: 'from-purple-400 to-purple-600',
  WorkUnit: 'from-rose-400 to-rose-600',
};

export function HomePage() {
  const { user } = useAuth();
  const [nodes, setNodes] = useState<BaselineNode[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getBaselineHierarchy()
      .then((result) => setNodes(result.items))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const stats = getStats(nodes);
  const greeting = getGreeting();

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="animate-fade-in-up">
        <h1 className="text-3xl font-bold text-gray-900 tracking-tight">
          {greeting}, {user?.name?.split(' ')[0] || 'there'}
        </h1>
        <p className="mt-1 text-gray-500">
          Manage your ISA-95 plant baseline hierarchy.
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat, i) => (
          <div
            key={stat.label}
            className="animate-fade-in-up glass-card rounded-2xl p-5 shadow-sm hover:shadow-md transition-shadow duration-300"
            style={{ animationDelay: `${i * 100}ms` }}
          >
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{stat.label}</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">
                  {loading ? (
                    <span className="inline-block h-8 w-12 rounded-md bg-gray-100 animate-shimmer" />
                  ) : (
                    stat.value
                  )}
                </p>
              </div>
              <div className={`flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br ${stat.color} shadow-md`}>
                <span className="text-white text-lg">{stat.icon}</span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions + Recent */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Quick Actions */}
        <div className="animate-fade-in-up lg:col-span-1" style={{ animationDelay: '400ms' }}>
          <h2 className="mb-4 text-sm font-semibold text-gray-900 uppercase tracking-wide">Quick Actions</h2>
          <div className="space-y-3">
            <Link
              to="/baseline"
              className="group flex items-center gap-4 rounded-2xl bg-white p-4 shadow-sm border border-gray-100 transition-all duration-200 hover:shadow-md hover:border-blue-200 hover:-translate-y-0.5"
            >
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-sm group-hover:shadow-md transition-shadow">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
              </div>
              <div>
                <p className="text-sm font-semibold text-gray-900">Add Baseline Node</p>
                <p className="text-xs text-gray-400">Create new ISA-95 entity</p>
              </div>
              <svg className="ml-auto h-4 w-4 text-gray-300 group-hover:text-blue-500 transition-colors" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </Link>
            <Link
              to="/baseline"
              className="group flex items-center gap-4 rounded-2xl bg-white p-4 shadow-sm border border-gray-100 transition-all duration-200 hover:shadow-md hover:border-emerald-200 hover:-translate-y-0.5"
            >
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-400 to-emerald-600 shadow-sm group-hover:shadow-md transition-shadow">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="5" r="3" />
                  <line x1="12" y1="8" x2="12" y2="14" />
                  <line x1="6" y1="14" x2="18" y2="14" />
                </svg>
              </div>
              <div>
                <p className="text-sm font-semibold text-gray-900">View Hierarchy</p>
                <p className="text-xs text-gray-400">Browse plant structure</p>
              </div>
              <svg className="ml-auto h-4 w-4 text-gray-300 group-hover:text-emerald-500 transition-colors" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="9 18 15 12 9 6" />
              </svg>
            </Link>
          </div>
        </div>

        {/* Recent Nodes */}
        <div className="animate-fade-in-up lg:col-span-2" style={{ animationDelay: '500ms' }}>
          <h2 className="mb-4 text-sm font-semibold text-gray-900 uppercase tracking-wide">Recent Nodes</h2>
          <div className="rounded-2xl bg-white shadow-sm border border-gray-100 overflow-hidden">
            {loading ? (
              <div className="p-8 text-center text-gray-400">
                <div className="mx-auto h-6 w-6 rounded-full border-2 border-blue-200 border-t-blue-600 animate-spin" />
              </div>
            ) : nodes.length === 0 ? (
              <div className="p-8 text-center text-gray-400">
                <p className="text-sm">No baseline nodes yet.</p>
                <Link to="/baseline" className="mt-2 inline-block text-xs font-medium text-blue-600 hover:text-blue-700">
                  Create your first node →
                </Link>
              </div>
            ) : (
              <div className="divide-y divide-gray-50">
                {nodes.slice(0, 5).map((node, i) => (
                  <div
                    key={node.id}
                    className="flex items-center gap-4 px-5 py-3.5 hover:bg-gray-50/50 transition-colors animate-slide-in-left"
                    style={{ animationDelay: `${i * 60}ms` }}
                  >
                    <div className={`flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br ${nodeTypeColors[node.nodeType] || 'from-gray-400 to-gray-500'} shadow-sm`}>
                      <span className="text-[10px] font-bold text-white">{node.nodeType.charAt(0)}</span>
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{node.displayName}</p>
                      <p className="text-[11px] text-gray-400">{node.nodeType} • v{node.version}</p>
                    </div>
                    <span className="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-medium text-emerald-600">
                      Active
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
}

function getStats(nodes: BaselineNode[]) {
  const types = nodes.reduce<Record<string, number>>((acc, n) => {
    acc[n.nodeType] = (acc[n.nodeType] || 0) + 1;
    return acc;
  }, {});

  return [
    { label: 'Total Nodes', value: nodes.length, color: 'from-blue-500 to-indigo-600', icon: '⬡' },
    { label: 'Sites', value: types['Site'] || 0, color: 'from-emerald-400 to-emerald-600', icon: '◉' },
    { label: 'Work Centers', value: types['WorkCenter'] || 0, color: 'from-amber-400 to-orange-500', icon: '⟡' },
    { label: 'Work Units', value: types['WorkUnit'] || 0, color: 'from-purple-400 to-purple-600', icon: '◈' },
  ];
}

