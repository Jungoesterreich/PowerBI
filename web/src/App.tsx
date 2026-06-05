import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import Uebersicht from './pages/Uebersicht';
import Pageviews from './pages/Pageviews';
import Downloads from './pages/Downloads';
import MediaAudio from './pages/MediaAudio';
import Heftreihen from './pages/Heftreihen';
import Sync from './pages/Sync';
import Warteschlange from './pages/Warteschlange';

const JUNGOE = '/websites/jungoe';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to={`${JUNGOE}/uebersicht`} replace />} />

        {/* Websites · jungoesterreich.at */}
        <Route path={`${JUNGOE}/uebersicht`} element={<Uebersicht />} />
        <Route path={`${JUNGOE}/heftreihen`} element={<Heftreihen />} />
        <Route path={`${JUNGOE}/pageviews`} element={<Pageviews />} />
        <Route path={`${JUNGOE}/downloads`} element={<Downloads />} />
        <Route path={`${JUNGOE}/media-audio`} element={<MediaAudio />} />

        {/* KSV · Warteschlange (Placeholder) */}
        <Route path="/ksv/warteschlange" element={<Warteschlange />} />

        {/* Admin */}
        <Route path="/admin/sync" element={<Sync />} />

        {/* Legacy redirects — preserve bookmarks from the flat layout */}
        <Route path="/uebersicht"  element={<Navigate to={`${JUNGOE}/uebersicht`}  replace />} />
        <Route path="/heftreihen"  element={<Navigate to={`${JUNGOE}/heftreihen`}  replace />} />
        <Route path="/pageviews"   element={<Navigate to={`${JUNGOE}/pageviews`}   replace />} />
        <Route path="/downloads"   element={<Navigate to={`${JUNGOE}/downloads`}   replace />} />
        <Route path="/media-audio" element={<Navigate to={`${JUNGOE}/media-audio`} replace />} />
        <Route path="/sync"        element={<Navigate to="/admin/sync"             replace />} />

        <Route path="*" element={<Navigate to={`${JUNGOE}/uebersicht`} replace />} />
      </Route>
    </Routes>
  );
}
