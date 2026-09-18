export type FileType = 'Original' | 'Light' | 'Filtered';

export type JobStatus =
  | 'Pending'
  | 'Processing'
  | 'Compressing'
  | 'Transcribing'
  | 'Summarizing'
  | 'Completed'
  | 'Failed';

export interface ApiError {
  code?: string | null;
  message?: string | null;
  details?: unknown;
}

export interface ApiResponse<T> {
  statusCode: number;
  response: T;
  error?: ApiError | null;
}

export interface AudioFileDto {
  id: number;
  fileName?: string | null;
  bucketPath?: string | null;
  fileSize?: number | null;
  durationSeconds?: number | null;
  format?: string | null;
  fileType: FileType;
  createdAt: string;
  jobId?: string | null;
  jobStatusDisplay?: string | null;
  /** URL lista para reproducir/descargar en el navegador (nueva en el OpenAPI). Preferila sobre bucketPath. */
  audioUrl?: string | null;
}

export interface AudioFileDtoPagedResult {
  items: AudioFileDto[] | null;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UploadResponse {
  jobId: string;
  audioId: number;
  fileName?: string | null;
  status: JobStatus;
}

export interface AudioDetailResponse extends AudioFileDto {
  lightFile: AudioFileDto;
  /** Archivo con filtrado de audio aplicado (nuevo). */
  filteredFile: AudioFileDto;
}

export type JobStepFlags =
  | 'None'
  | 'Uploaded'
  | 'Compressed'
  | 'Transcribed'
  | 'Summarized'
  | 'Filtered';

export interface JobStatusResponse {
  jobId: string;
  statusDisplay?: string | null;
  status: JobStatus;
  completedSteps: JobStepFlags;
  createdAt: string;
  updatedAt: string;
  originalFile: AudioFileDto;
  lightFile: AudioFileDto;
  /** Archivo con filtrado de audio aplicado (nuevo). */
  filteredFile: AudioFileDto;
}

export const JOB_STATUS_OPTIONS: JobStatus[] = [
  'Pending',
  'Processing',
  'Compressing',
  'Transcribing',
  'Summarizing',
  'Completed',
  'Failed',
];

export interface GetAudioListParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  format?: string;
}
