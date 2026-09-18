import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('error_rate');
const uploadDuration = new Trend('upload_duration', true);
const statusDuration = new Trend('status_duration', true);

export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 10 },
    { duration: '2m', target: 20 },
    { duration: '1m', target: 30 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    'upload_duration': ['p(95)<5000'],
    'error_rate': ['rate<0.01'],
    'http_req_duration': ['p(95)<3000'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5247';
const TEST_FILE = __ENV.TEST_FILE || 'sample.mp3';
const WAIT_TIME = parseInt(__ENV.WAIT_TIME) || 2;
const BATCH_SIZE = parseInt(__ENV.BATCH_SIZE) || 10;

const fileData = open(TEST_FILE, 'b');

export default function () {
  const uploadRes = http.post(
    `${BASE_URL}/api/audio`,
    {
      files: http.file(fileData, 'audio.mp3', 'audio/mpeg'),
    },
    {
      tags: { name: 'upload' },
    }
  );

  uploadDuration.add(uploadRes.timings.duration);
  errorRate.add(uploadRes.status !== 200);

  const body = uploadRes.json();
  const jobId = body.response && body.response[0] && body.response[0].jobId;

  const uploadOk = check(uploadRes, {
    'upload status is 200': (r) => r.status === 200,
    'upload has jobId': () => jobId !== undefined,
  });

  if (!uploadOk) {
    console.warn(`Upload failed or missing jobId: ${uploadRes.status} ${uploadRes.body}`);
    return;
  }

  sleep(WAIT_TIME);

  const statusRes = http.get(`${BASE_URL}/api/jobs/${jobId}/status`, {
    tags: { name: 'status' },
  });

  statusDuration.add(statusRes.timings.duration);
  errorRate.add(statusRes.status !== 200);

  check(statusRes, {
    'status is 200': (r) => r.status === 200,
    'status has status': (r) => r.json('response.status') !== undefined,
  });

  const listRes = http.get(`${BASE_URL}/api/audio?page=1&pageSize=${BATCH_SIZE}`, {
    tags: { name: 'list' },
  });

  errorRate.add(listRes.status !== 200);

  check(listRes, {
    'list status is 200': (r) => r.status === 200,
    'list has items': (r) => r.json('response.items') !== undefined,
  });

  sleep(1);
}