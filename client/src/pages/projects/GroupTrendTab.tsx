import { fetchGroupTrend } from '../../api/analyticsApi';
import { GroupPlaceholderTab } from './GroupPlaceholderTab';

export function GroupTrendTab({ projectId }: { projectId: number }) {
  return (
    <GroupPlaceholderTab
      projectId={projectId}
      title="Тренд по группам"
      fetcher={fetchGroupTrend}
    />
  );
}
