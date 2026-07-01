document.addEventListener('DOMContentLoaded', () => {
    
  document.getElementById('year').textContent = new Date().getFullYear();

  const tbody = document.getElementById('deviceTable');
  const sorted = [...DATA.devices].sort((a,b)=>b.usage - a.usage);
  sorted.forEach((d, idx) => {
    const tr = document.createElement('tr');
    if (idx === 0) tr.classList.add('top-row');
    tr.innerHTML = `<td>${d.name}</td><td>${d.usage}</td>`;
    tbody.appendChild(tr);
  });
});
const fmt = (n, d = 1) =>
  (Number(n) || 0).toLocaleString(undefined, { minimumFractionDigits: d, maximumFractionDigits: d });

const total = (DATA.devices || []).reduce((sum, d) => sum + (Number(d.usage) || 0), 0);
const totalCell = document.getElementById('deviceTotalCell');

if (totalCell) {
  totalCell.textContent = `${fmt(total, 1)} kWh`; 
} else {
  const tbody = document.getElementById('deviceTable');
  const tr = document.createElement('tr');
  tr.className = 'total-row';
  tr.innerHTML = `<th scope="row">Total device energy</th><td>${fmt(total, 1)} kWh</td>`;
  tbody.appendChild(tr);
}