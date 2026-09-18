import axios from 'axios';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || '';

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
});

// El backend envuelve toda respuesta exitosa en { statusCode, response, error }.
// Desenvolvemos acá una sola vez para que el resto del código pueda tratar
// response.data como el payload real, en vez de repetir response.data.response
// en cada llamada.
apiClient.interceptors.response.use((response) => {
  if (response.data && typeof response.data === 'object' && 'response' in response.data) {
    response.data = response.data.response;
  }
  return response;
});