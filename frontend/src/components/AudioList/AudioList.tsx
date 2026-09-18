import React, { useEffect, useState } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Typography,
  CircularProgress,
  IconButton,
  Tooltip,
} from '@mui/material';
import { Visibility } from '@mui/icons-material';
import { audioApi } from '../../api/endpoints';
import type { AudioFile } from '../../types';
import styles from './AudioList.module.scss';

export const AudioList: React.FC = () => {
  const [audioFiles, setAudioFiles] = useState<AudioFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAudioFiles = async () => {
      try {
        const response = await audioApi.getAudioFiles({ page: 1, pageSize: 20 });
        setAudioFiles(response.data.items);
      } catch (err: any) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    fetchAudioFiles();
  }, []);

  if (loading) {
    return (
      <div className={styles.loading}>
        <CircularProgress />
        <Typography>Cargando archivos...</Typography>
      </div>
    );
  }

  if (error) {
    return (
      <Typography color="error" variant="body1">
        Error al cargar archivos: {error}
      </Typography>
    );
  }

  return (
    <Paper elevation={3} className={styles.container}>
      <Typography variant="h6" gutterBottom>
        Archivos de Audio
      </Typography>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Nombre</TableCell>
              <TableCell>Tamaño</TableCell>
              <TableCell>Formato</TableCell>
              <TableCell>Estado</TableCell>
              <TableCell>Acciones</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {audioFiles.map((file) => (
              <TableRow key={file.id}>
                <TableCell>{file.originalFileName}</TableCell>
                <TableCell>
                  {file.fileSize ? (file.fileSize / 1024).toFixed(1) + ' KB' : '-'}
                </TableCell>
                <TableCell>{file.format?.toUpperCase() || '-'}</TableCell>
                <TableCell>
                  {file.jobStatus && (
                    <Chip
                      label={file.jobStatus}
                      color={file.jobStatus === 'Completed' ? 'success' : 'warning'}
                      size="small"
                    />
                  )}
                </TableCell>
                <TableCell>
                  {file.jobId && (
                    <Tooltip title="Ver estado del Job">
                      <IconButton size="small" href={`#job-${file.jobId}`}>
                        <Visibility fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  );
};