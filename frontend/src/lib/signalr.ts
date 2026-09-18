import * as signalR from '@microsoft/signalr';

const HUB_URL = import.meta.env.VITE_HUB_BASE_URL ?? 'http://localhost:5248/hubs/audio-processing';

export type JobHubEvent =
  | 'TranscriptionCompleted'
  | 'SummaryCompleted'
  | 'JobCompleted'
  | 'JobFailed';

export interface JobHubPayload {
  jobId: string;
  message: string;
}

const JOB_HUB_EVENTS: JobHubEvent[] = [
  'TranscriptionCompleted',
  'SummaryCompleted',
  'JobCompleted',
  'JobFailed',
];

export function createJobHubConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, { withCredentials: false })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}

export { JOB_HUB_EVENTS };
