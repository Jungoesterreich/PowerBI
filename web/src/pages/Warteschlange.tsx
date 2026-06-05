import PageHeader from '../components/PageHeader';
import Placeholder from '../components/Placeholder';

export default function Warteschlange() {
  return (
    <div>
      <PageHeader
        title="Warteschlange"
        sub="3CX-Anrufstatistik · KSV — Quelle für die ehemalige Power-BI-Auswertung"
      />
      <div className="panel panel-pad mt-6" style={{ minHeight: 280 }}>
        <Placeholder
          mode="empty"
          title="In Vorbereitung"
          hint="Die Anbindung an die 3CX-Telefonanlage und der Importer/Sync für Queue-Daten sind geplant. Bis dahin bleibt dieser Bereich leer."
        />
      </div>
    </div>
  );
}
