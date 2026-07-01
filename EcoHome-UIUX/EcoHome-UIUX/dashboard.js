document.addEventListener('DOMContentLoaded', () => {
  
  const DATA = window.DATA;
  if (!DATA) {
    console.error('[dashboard.js] DATA missing — is data.js loaded before this file?');
    return;
  }


  const yearEl = document.getElementById('year');
  if (yearEl) yearEl.textContent = new Date().getFullYear();

  const fmt = (n, digits = 0) =>
    (typeof n === 'number' ? n : 0).toLocaleString(undefined, {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits
    });

  const elecTotal = (DATA.weekUsage || []).reduce((a, b) => a + b, 0);
  const topDevice = [...(DATA.devices || [])].sort((a, b) => b.usage - a.usage)[0];

  const firstHalfElec = (DATA.weekUsage || []).slice(0, 3).reduce((a, b) => a + b, 0);
  const secondHalfElec = (DATA.weekUsage || []).slice(-3).reduce((a, b) => a + b, 0);
  const elecTrend =
    secondHalfElec > firstHalfElec ? 'increasing' :
    secondHalfElec < firstHalfElec ? 'decreasing' : 'stable';

  const mTotal = document.getElementById('metric-total');
  const mTrend = document.getElementById('metric-trend');
  const mTopName = document.getElementById('metric-top-device');
  const mTopUsage = document.getElementById('metric-top-usage');

  if (mTotal) mTotal.textContent = `${fmt(elecTotal)} kWh`;
  if (mTrend) mTrend.textContent = `Trend: ${elecTrend}`;
  if (mTopName) mTopName.textContent = topDevice?.name ?? '—';
  if (mTopUsage) mTopUsage.textContent = `${fmt(topDevice?.usage)} kWh`;

  const waterTotal = (DATA.waterWeekLitres || []).reduce((a, b) => a + b, 0);
  const firstHalfWater = (DATA.waterWeekLitres || []).slice(0, 3).reduce((a, b) => a + b, 0);
  const secondHalfWater = (DATA.waterWeekLitres || []).slice(-3).reduce((a, b) => a + b, 0);
  const waterTrend =
    secondHalfWater > firstHalfWater ? 'increasing' :
    secondHalfWater < firstHalfWater ? 'decreasing' : 'stable';

  const elWaterTotal = document.getElementById('metric-water-total');
  const elWaterTrend = document.getElementById('metric-water-trend');
  if (elWaterTotal) elWaterTotal.textContent = `${fmt(waterTotal)} L`;
  if (elWaterTrend) elWaterTrend.textContent = `Trend: ${waterTrend}`;

  const factor = Number(DATA.co2FactorKgPerKWh) || 0;
  const co2TotalKg = elecTotal * factor;
  const elCO2 = document.getElementById('metric-co2-total');
  const elCO2Note = document.getElementById('metric-co2-note');
  if (elCO2) elCO2.textContent = `${fmt(co2TotalKg, 1)} kg`;
  if (elCO2Note) elCO2Note.textContent = 'From electricity only';

const canvas = document.getElementById('usageChart');
if (canvas && window.Chart) {
  const ctx2d = canvas.getContext('2d');

  const vals = DATA.weekUsage || [];
  const minY = Math.min(...vals);
  const maxY = Math.max(...vals);
  const span = Math.max(1e-9, maxY - minY);



  const colorForY = (y, alpha = 1) => {
    const t = (y - minY) / span;                
    const hue = 270 * (1 - t);                 
    return `hsla(${hue}, 90%, 50%, ${alpha})`;
  };

  // Background rainbow "heat" (vertical gradient across the chart area)
  const heatmapBackground = {
    id: 'heatmapBackground',
    beforeDatasetsDraw(chart) {
      const { ctx, chartArea } = chart;
      if (!chartArea) return;
      const g = ctx.createLinearGradient(0, chartArea.bottom, 0, chartArea.top);
      g.addColorStop(0.00, 'hsla(270,90%,50%,0.10)'); // violet
      g.addColorStop(0.16, 'hsla(240,90%,50%,0.10)'); // blue
      g.addColorStop(0.33, 'hsla(200,90%,50%,0.10)'); // cyan
      g.addColorStop(0.50, 'hsla(120,90%,45%,0.10)'); // green
      g.addColorStop(0.66, 'hsla(60, 95%,50%,0.10)'); // yellow
      g.addColorStop(0.83, 'hsla(30, 95%,50%,0.10)'); // orange
      g.addColorStop(1.00, 'hsla(0,  95%,50%,0.10)'); // red
      ctx.save();
      ctx.fillStyle = g;
      ctx.fillRect(chartArea.left, chartArea.top,
                   chartArea.right - chartArea.left,
                   chartArea.bottom - chartArea.top);
      ctx.restore();
    }
  };

  new Chart(ctx2d, {
    type: 'line',
    data: {
      labels: DATA.days,
      datasets: [{
        label: 'kWh',
        data: vals,
        tension: 0.35,
        fill: true,
        borderWidth: 3,
        
        segment: {
          borderColor: (seg) => {
            const y0 = seg.p0?.parsed?.y ?? minY;
            const y1 = seg.p1?.parsed?.y ?? minY;
            return colorForY((y0 + y1) / 2);
          }
        },
        
        pointRadius: 3,
        pointHoverRadius: 5,
        pointBackgroundColor: (ctx) => colorForY(ctx.parsed.y),
        pointBorderWidth: 0,
        
        backgroundColor: (ctx) => {
          const { chart } = ctx;
          const area = chart.chartArea;
          if (!area) return 'rgba(0,0,0,0.08)';
          const g = chart.ctx.createLinearGradient(0, area.bottom, 0, area.top);
          g.addColorStop(0.00, 'hsla(270,90%,50%,0.12)');
          g.addColorStop(0.16, 'hsla(240,90%,50%,0.12)');
          g.addColorStop(0.33, 'hsla(200,90%,50%,0.12)');
          g.addColorStop(0.50, 'hsla(120,90%,45%,0.12)');
          g.addColorStop(0.66, 'hsla(60, 95%,50%,0.12)');
          g.addColorStop(0.83, 'hsla(30, 95%,50%,0.12)');
          g.addColorStop(1.00, 'hsla(0,  95%,50%,0.12)');
          return g;
        }
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: { mode: 'index', intersect: false },
      plugins: {
        legend: { labels: { boxWidth: 12 } }
      },
      scales: {
        y: { beginAtZero: true, title: { display: true, text: 'kWh' } },
        x: { title: { display: true, text: 'Day' } }
      }
    },
    plugins: [heatmapBackground]
  });
} else {
  console.warn('[dashboard.js] Chart.js not found or #usageChart missing.');
}
});
