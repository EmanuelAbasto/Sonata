import React, { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import {
  Box,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Chip,
  CircularProgress,
  Alert,
  Paper,
} from '@mui/material';
import { CloudUpload, Cancel } from '@mui/icons-material';
import styles from './UploadForm.module.scss';
import { useAudioUpload } from '../../hooks/useAudioUpload';

export const UploadForm: React.FC = () => {
  const [files, setFiles] = useState<File[]>([]);
  const [target, setTarget] = useState<string>('original');
  const { uploadFiles, loading, results, error } = useAudioUpload();

  const onDrop = useCallback((acceptedFiles: File[]) => {
    setFiles((prev) => [...prev, ...acceptedFiles]);
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'audio/*': ['.mp3', '.wav', '.m4a', '.aac', '.flac'],
    },
  });

  const removeFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (files.length === 0) return;
    await uploadFiles(files, target);
    if (!error) {
      setFiles([]);
    }
  };

  return (
    <Paper elevation={3} className={styles.container}>
      <Typography variant="h5" gutterBottom>
        Subir Audios
      </Typography>

      <div
        {...getRootProps()}
        className={`${styles.dropzone} ${isDragActive ? styles.active : ''}`}
      >
        <input {...getInputProps()} />
        <CloudUpload fontSize="large" color="primary" />
        <Typography variant="body1" color="textSecondary">
          {isDragActive
            ? 'Suelta los archivos aquí...'
            : 'Arrastra y suelta archivos de audio, o haz clic para seleccionar'}
        </Typography>
        <Typography variant="caption" color="textSecondary">
          Formatos soportados: MP3, WAV, M4A, AAC, FLAC
        </Typography>
      </div>

      {files.length > 0 && (
        <Box className={styles.fileList}>
          {files.map((file, index) => (
            <Chip
              key={index}
              label={`${file.name} (${(file.size / 1024).toFixed(1)} KB)`}
              onDelete={() => removeFile(index)}
              deleteIcon={<Cancel />}
              className={styles.chip}
            />
          ))}
        </Box>
      )}

      <form onSubmit={handleSubmit} className={styles.form}>
        <FormControl fullWidth variant="outlined" size="small" sx={{ mb: 2 }}>
          <InputLabel id="target-label">Target</InputLabel>
          <Select
            labelId="target-label"
            value={target}
            onChange={(e) => setTarget(e.target.value)}
            label="Target"
          >
            <MenuItem value="original">Original</MenuItem>
            <MenuItem value="lightweight">Lightweight</MenuItem>
          </Select>
        </FormControl>

        <Button
          type="submit"
          variant="contained"
          color="primary"
          disabled={files.length === 0 || loading}
          fullWidth
        >
          {loading ? <CircularProgress size={24} color="inherit" /> : 'Subir Audios'}
        </Button>
      </form>

      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {results.length > 0 && (
        <Alert severity="success" sx={{ mt: 2 }}>
          {results.length} archivo(s) subido(s) correctamente. Job IDs: {results.map(r => r.jobId).join(', ')}
        </Alert>
      )}
    </Paper>
  );
};