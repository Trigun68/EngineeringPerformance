import { fetchGroupAnalysis } from '../../api/analyticsApi';
import { GroupPlaceholderTab } from './GroupPlaceholderTab';

export function GroupAnalysisTab({ projectId }: { projectId: number }) {
  return (
    <GroupPlaceholderTab
      projectId={projectId}
      title="Групповой анализ"
      fetcher={fetchGroupAnalysis}
    />
  );
}
