document.addEventListener('DOMContentLoaded', () => {
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  document.querySelectorAll('[data-promo-media]').forEach(frame => {
    const video = frame.querySelector('video');
    const soundButton = frame.querySelector('.promo-sound-toggle');
    if (!video) return;
    if (reduceMotion) { video.autoplay = false; video.pause(); video.controls = true; }
    soundButton?.addEventListener('click', () => {
      video.muted = !video.muted;
      soundButton.textContent = video.muted ? 'เปิดเสียง' : 'ปิดเสียง';
    });
    const observer = new IntersectionObserver(entries => entries.forEach(entry => {
      if (!entry.isIntersecting) video.pause();
      else if (!reduceMotion && frame.dataset.autoplay === 'true') video.play().catch(() => {});
    }), { threshold: .25 });
    observer.observe(frame);
  });
});
