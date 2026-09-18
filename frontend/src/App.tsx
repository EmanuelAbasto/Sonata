import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { Container, AppBar, Toolbar, Typography, Button } from '@mui/material';
import { UploadForm } from './components/UploadForm/UploadForm';
import { AudioList } from './components/AudioList/AudioList';
import { JobStatus } from './components/JobStatus/JobStatus';
import './App.scss';

function App() {
  return (
    <BrowserRouter>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Audio Uploader
          </Typography>
          <Button color="inherit" component={Link} to="/">
            Subir
          </Button>
          <Button color="inherit" component={Link} to="/list">
            Archivos
          </Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ mt: 4 }}>
        <Routes>
          <Route path="/" element={<UploadForm />} />
          <Route path="/list" element={<AudioList />} />
          <Route path="/job/:jobId" element={<JobStatus />} />
        </Routes>
      </Container>
    </BrowserRouter>
  );
}

export default App;