// Live Data from joedigi — with batched fetching

const axios = require('axios').default;
const fs = require('fs');

// Config
const pathToFolder = 'D:\\datawarehouse/data/matomo-joedigi-visits-details/';
const site = 'joedigi';
const idSite = 5;
const token = '44ba87a0b2ab361bafd779682f9d252c';
const batchSize = 1000;

// Yesterday's date
const d = new Date();
d.setDate(d.getDate() - 1);
const xd = d.toISOString().slice(0, 10);
const year = xd.substring(0, 4);
const yearShort = xd.substring(2, 4);
const month = xd.substring(5, 7);
const day = xd.substring(8, 10);
const date = `${year}-${month}-${day}`;
const dateInFilename = `${yearShort}${month}${day}`;

async function fetchAllVisits() {
  let allVisits = [];
  let offset = 0;

  while (true) {
    const url = `https://jungoesterreich.matomo.cloud/index.php?module=API&method=Live.getLastVisitsDetails&idSite=${idSite}&period=day&date=${date}&format=json&token_auth=${token}&filter_limit=${batchSize}&filter_offset=${offset}`;

    console.log(`Fetching offset=${offset} ...`);
    const response = await axios({ method: 'get', url, responseType: 'json' });
    const batch = response.data;

    if (!Array.isArray(batch) || batch.length === 0) {
      break;
    }

    allVisits = allVisits.concat(batch);
    console.log(`  Got ${batch.length} visits (total: ${allVisits.length})`);

    if (batch.length < batchSize) {
      break;
    }

    offset += batchSize;
  }

  return allVisits;
}

(async () => {
  try {
    const visits = await fetchAllVisits();
    const fileName = `${pathToFolder}matomo-${site}-VisitsDetails-${dateInFilename}.json`;
    console.log(`Writing ${visits.length} visits to ${fileName}`);
    fs.writeFileSync(fileName, JSON.stringify(visits));
    console.log('Done.');
  } catch (error) {
    console.error('Error:', error.message);
  }
})();
