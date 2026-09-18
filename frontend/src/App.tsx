import { Route, Routes } from 'react-router-dom';
import { Layout } from '@/components/Layout';
import { GlobalJobNotifications } from '@/components/GlobalJobNotifications';
import { AudioLibraryProvider } from '@/context/AudioLibraryContext';
import { ToastProvider } from '@/context/ToastContext';
import UploadPage from '@/pages/UploadPage';
import SearchPage from '@/pages/SearchPage';
import AudioDetailPage from '@/pages/AudioDetailPage';

export default function App() {
  return (
    <ToastProvider>
      <AudioLibraryProvider>
        <GlobalJobNotifications />
        <Layout>
          <Routes>
            <Route path="/" element={<UploadPage />} />
            <Route path="/search" element={<SearchPage />} />
            <Route path="/audio/:audioId" element={<AudioDetailPage />} />
          </Routes>
        </Layout>
      </AudioLibraryProvider>
    </ToastProvider>
  );
}
