export interface AudioFile {
  id: number;
  originalFileName: string;
  bucketPath: string;
  fileSize?: number;
  durationSeconds?: number;
  format?: string;
  fileType: string;
  createdAt: string;
  jobStatus?: string;
  jobId?: string;
}

export interface UploadResponse {
  jobId: string;
  fileName: string;
  status: string;
}

export interface JobStatusResponse {
  jobId: string;
  status: string;
  text?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}