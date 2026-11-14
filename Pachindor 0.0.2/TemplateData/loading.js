(() => {
  const overlay = document.querySelector('#loading');
  const canvas = document.querySelector('#unity-canvas');
  const fill = document.querySelector('#fill');
  const progress = document.querySelector('#progress');
  const status = document.querySelector('#status');
  const percent = document.querySelector('#percent');
  const error = document.querySelector('#error');
  const retry = document.querySelector('#retry');
  let failed = false;
  let value = 0;
  retry.addEventListener('click', () => window.location.reload());
  window.agtLoading = {
    progress(amount) {
      if (failed || !Number.isFinite(amount)) return;
      value = Math.max(value, Math.min(1, Math.max(0, amount)));
      const whole = Math.floor(value * 100);
      fill.style.width = `${value * 100}%`;
      progress.setAttribute('aria-valuenow', String(whole));
      percent.textContent = `${whole}%`;
      if (value >= 1) {
        status.textContent = 'Preparing…';
        status.classList.remove('sr-only');
      }
    },
    ready() {
      if (failed) return;
      window.agtLoading.progress(1);
      overlay.classList.add('leaving');
      // Always remove the overlay, including when reduced motion disables animation.
      window.setTimeout(() => {
        if (failed) return;
        overlay.hidden = true;
        canvas.focus({ preventScroll: true });
      }, window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 200);
    },
    fail(message) {
      failed = true;
      overlay.hidden = false;
      overlay.classList.remove('leaving');
      overlay.classList.add('failed');
      status.textContent = 'Unable to load';
      status.classList.remove('sr-only');
      percent.textContent = '!';
      error.textContent = String(message || 'Unable to start the game. Please retry.');
      error.hidden = false;
      retry.hidden = false;
    }
  };
})();
