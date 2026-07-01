
(function () {
  window.DATA = {
    days: ["Mon","Tue","Wed","Thu","Fri","Sat","Sun"],
    weekUsage: [3, 8.4, 6.7, 0.3, 6, 9.2, 11.4],
    waterWeekLitres: [320, 300, 280, 350, 310, 290, 330],
    devices: [
    { name: "Heater",           usage: 2.3 },
    { name: "Boiler",           usage: 1.6 },
    { name: "Fridge",           usage: 1.1 },
    { name: "TV",               usage: 0.9 },
    { name: "Washing Machine",  usage: 0.8 }
],
    co2FactorKgPerKWh: 0.233
  };
  console.log("[data.js] DATA loaded:", window.DATA);
})();