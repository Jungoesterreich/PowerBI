import { Outlet, useLocation } from 'react-router-dom';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import GlobalFilterBar from './GlobalFilterBar';
import BrandStrip from './BrandStrip';

export default function Layout() {
  const { pathname } = useLocation();
  // GlobalFilterBar (Zeitraum/Schuljahr) and BrandStrip (Marken) are Matomo-/
  // jungoesterreich.at-specific. They are noise on KSV/Sales/Admin pages.
  const showWebsiteBars = pathname.startsWith('/websites/');

  return (
    <div
      className="app flex min-h-screen"
      style={{ background: 'var(--bg)', color: 'var(--fg)', minWidth: 1180 }}
    >
      <Sidebar />
      <main className="flex-1 min-w-0 flex flex-col">
        <Topbar />
        {showWebsiteBars && <GlobalFilterBar />}
        {showWebsiteBars && <BrandStrip />}
        <div className="px-8 py-6 flex-1">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
