document.addEventListener('DOMContentLoaded', () => {

  const data = {
    weekUsage: [3, 8.4, 7.6, 0.3, 6, 9.2, 11.4],
    days: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
    devices: [
        { name: "Heater", usage: 2.3 },
        { name: "Boiler", usage: 1.6 },
        { name: "Fridge", usage: 1.1 },
        { name: "TV", usage: 0.9 },
        { name: "Washing Machine", usage: 0.8 }
    ]
  };
  const chartCanvas = document.getElementById('usageChart');
  if (chartCanvas && window.Chart) {
    const ctx = chartCanvas.getContext('2d');
    new Chart(ctx, {
      type: 'line',
      data: {
        labels: data.days,
        datasets: [{
          label: 'kWh',
          data: data.weekUsage,
          fill: true
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        plugins: { legend: { display: true } },
        scales: {
          y: { beginAtZero: true }
        }
      }
    });
  } else {
    chartCanvas?.insertAdjacentHTML(
      'afterend',
      `<p role="alert">Chart unavailable (Chart.js not loaded).</p>`
    );
  }
  const tbody = document.getElementById('deviceTable');
  if (tbody) {

    const sorted = [...data.devices].sort((a, b) => b.usage - a.usage);
    for (const d of sorted) {
      const tr = document.createElement('tr');
      tr.innerHTML = `<td>${d.name}</td><td>${d.usage}</td>`;
      tbody.appendChild(tr);
    }
  }
});
