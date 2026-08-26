import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material';
import AssessmentIcon from '@mui/icons-material/Assessment';
import PersonIcon from '@mui/icons-material/Person';
import BusinessIcon from '@mui/icons-material/Business';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { Project } from '../../../../types/project';
import type { ProjectVersion } from '../../../../api/projectVersionsApi';
import { DIRECTION_LABEL } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface ReportHeaderProps {
  project: Project;
  version: ProjectVersion;
  onExportPdf?: () => void;
  exporting?: boolean;
}

export function ReportHeader({ project, version, onExportPdf, exporting }: ReportHeaderProps) {
  const versionDate = version.createdAt
    ? new Date(version.createdAt).toLocaleDateString('ru-RU')
    : null;

  return (
    <Paper elevation={0} variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
      <Stack spacing={3}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Box
            sx={{
              width: 36,
              height: 36,
              borderRadius: 1.5,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: wpColors.accent,
              color: '#FFFFFF',
              flexShrink: 0,
            }}
          >
            <AssessmentIcon fontSize="small" />
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography
              variant="caption"
              sx={{ color: wpColors.muted, letterSpacing: 0.4, textTransform: 'uppercase', fontWeight: 600 }}
            >
              Анализ версии проекта
            </Typography>
            <Typography variant="h6" sx={{ color: wpColors.heading, fontWeight: 600, lineHeight: 1.25 }}>
              Отчёт по инженерной производительности
            </Typography>
          </Box>
          {onExportPdf && (
            <Button
              variant="outlined"
              size="small"
              disabled={exporting}
              onClick={onExportPdf}
              startIcon={<PictureAsPdfIcon />}
              sx={{ textTransform: 'none', flexShrink: 0 }}
            >
              Экспорт PDF
            </Button>
          )}
        </Stack>

        <Stack
          direction={{ xs: 'column', lg: 'row' }}
          spacing={2}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', lg: 'flex-end' }}
        >
          <Stack spacing={1}>
            <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
              {project.name}
            </Typography>
            <Stack direction="row" spacing={2.5} flexWrap="wrap" useFlexGap alignItems="center">
              <Typography
                variant="caption"
                sx={{ fontFamily: 'monospace', color: wpColors.muted }}
              >
                {project.code}
              </Typography>
              {project.gip && (
                <Stack direction="row" spacing={0.5} alignItems="center">
                  <PersonIcon sx={{ fontSize: 15, color: wpColors.muted }} />
                  <Typography variant="body2" sx={{ color: wpColors.muted }}>
                    ГИП: {project.gip}
                  </Typography>
                </Stack>
              )}
              {project.customer && (
                <Stack direction="row" spacing={0.5} alignItems="center">
                  <BusinessIcon sx={{ fontSize: 15, color: wpColors.muted }} />
                  <Typography variant="body2" sx={{ color: wpColors.muted }}>
                    {project.customer}
                  </Typography>
                </Stack>
              )}
            </Stack>
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              icon={<CalendarMonthIcon sx={{ fontSize: 15 }} />}
              label={`${version.versionName}${versionDate ? ` · ${versionDate}` : ''}`}
              sx={{ bgcolor: wpColors.pageBg }}
            />
            <Chip size="small" variant="outlined" label={DIRECTION_LABEL[version.direction] ?? version.name} />
            {project.workStatus && <Chip size="small" variant="outlined" label={project.workStatus} />}
            {version.isArchived && (
              <Chip size="small" label="Архив" sx={{ bgcolor: wpColors.muted, color: '#FFFFFF' }} />
            )}
          </Stack>
        </Stack>
      </Stack>
    </Paper>
  );
}
