// Live Data monthly — with batched fetching per day

const axios = require('axios').default;
const fs = require('fs');

// Config — adjust these before running
const pathToFolder = 'D:\\datawarehouse/data/';
const site = 'topicdigi';
const idSite = 6;
const token = '44ba87a0b2ab361bafd779682f9d252c';
const batchSize = 1000;

const year = '2024';
const month = '10';
const days = [
  '01','02','03','04','05','06','07','08','09','10',
  '11','12','13','14','15','16','17','18','19','20',
  '21','22','23','24','25','26','27','28','29','30','31'
];
const yearShort = year.substring(2);

async function fetchAllVisitsForDate(date) {
  let allVisits = [];
  let offset = 0;

  while (true) {
    const url = `https://jungoesterreich.matomo.cloud/index.php?module=API&method=Live.getLastVisitsDetails&idSite=${idSite}&period=day&date=${date}&format=json&token_auth=${token}&filter_limit=${batchSize}&filter_offset=${offset}`;

    const response = await axios({ method: 'get', url, responseType: 'json' });
    const batch = response.data;

    if (!Array.isArray(batch) || batch.length === 0) {
      break;
    }

    allVisits = allVisits.concat(batch);

    if (batch.length < batchSize) {
      break;
    }

    offset += batchSize;
  }

  return allVisits;
}

(async () => {
  for (const day of days) {
    const date = `${year}-${month}-${day}`;
    const dateInFilename = `${yearShort}${month}${day}`;

    try {
      console.log(`Fetching ${date} ...`);
      const visits = await fetchAllVisitsForDate(date);
      const fileName = `${pathToFolder}matomo-${site}-VisitsDetails-${dateInFilename}.json`;
      console.log(`  ${visits.length} visits -> ${fileName}`);
      fs.writeFileSync(fileName, JSON.stringify(visits));
    } catch (error) {
      console.error(`  Error for ${date}: ${error.message}`);
    }
  }

  console.log('Done.');
})();
