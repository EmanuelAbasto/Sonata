import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Paper, Typography, CircularProgress, Box } from '@mui/material';
import { audioApi } from '../../api/endpoints';
import type { JobStatusResponse } from '../../types';

export const JobStatus: React.FC = () => {
  const { jobId } = useParams<{ jobId: string }>();
  const [job, setJob] = useState<JobStatusResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!jobId) return;
    const fetchJob = async () => {
      try {
        const response = await audioApi.getJobStatus(jobId);
        setJob(response.data);
      } catch (err: any) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    fetchJob();
  }, [jobId]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '200px' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Typography color="error" variant="body1">
        Error: {error}
      </Typography>
    );
  }

  return (
    <Paper elevation={3} sx={{ p: 3, maxWidth: 500, mx: 'auto', mt: 4 }}>
      <Typography variant="h6">Job {job?.jobId}</Typography>
      <Typography variant="body2">Estado: {job?.status}</Typography>
      {job?.text && (
        <Typography variant="body2" sx={{ mt: 2 }}>
          Resultado: {job.text}
        </Typography>
      )}
      <Typography variant="caption" sx={{ display: 'block', color: 'textSecondary' }}>
        Creado: {new Date(job?.createdAt || '').toLocaleString()}
      </Typography>
      <Typography variant="caption" sx={{ display: 'block', color: 'textSecondary' }}>
        Actualizado: {new Date(job?.updatedAt || '').toLocaleString()}
      </Typography>
    </Paper>
  );
};