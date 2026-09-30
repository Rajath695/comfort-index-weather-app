const toggleBtn = document.getElementById('theme-toggle');

toggleBtn.addEventListener('click', () => {
  const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
  document.documentElement.setAttribute('data-theme', isDark ? 'light' : 'dark');
  toggleBtn.textContent = isDark ? '🌙 Dark' : '☀️ Light';
});




async function loadComfortIndex() {
  const loading = document.getElementById('loading');
  const container = document.getElementById('cities');

  try {
    const res = await fetch('/api/comfort-index');
    const data = await res.json();

    loading.style.display = 'none';

    container.innerHTML = data.map(city => `
      <div class="card">
        <span class="rank">Rank #${city.rank}</span>
        <h2>${city.cityName}</h2>
        <p>${city.description}</p>
        <p>${city.tempCelsius}°C</p>
        <div class="score">${city.comfortScore}</div>
      </div>
    `).join('');
  } catch (err) {
    loading.textContent = 'Failed to load weather data.';
    console.error(err);
  }
}

loadComfortIndex();