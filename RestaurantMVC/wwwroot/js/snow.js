// Simple Snow Effect for Restaurantly
// Lightweight canvas-based snowflakes. Provides window.enableSnow() / window.disableSnow().
(function() {
  let canvas, ctx, flakes = [], animationId = null, enabled = false;

  function createCanvas() {
    canvas = document.createElement('canvas');
    canvas.id = 'snow-canvas';
    canvas.style.position = 'fixed';
    canvas.style.top = '0';
    canvas.style.left = '0';
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.style.pointerEvents = 'none';
    canvas.style.zIndex = '9999';
    document.body.appendChild(canvas);
    ctx = canvas.getContext('2d');
    onResize();
    window.addEventListener('resize', onResize);
  }

  function onResize() {
    if (!canvas) return;
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
  }

  function initFlakes() {
    flakes = [];
    const isMobile = /Mobi|Android/i.test(navigator.userAgent);
    const count = isMobile ? 60 : 120;
    for (let i = 0; i < count; i++) {
      flakes.push(makeFlake(true));
    }
  }

  function makeFlake(randomY) {
    const size = Math.random() * 2 + 1; // 1-3px
    return {
      x: Math.random() * canvas.width,
      y: randomY ? Math.random() * canvas.height : -10,
      r: size,
      d: Math.random() * 0.5 + 0.5, // fall speed
      w: (Math.random() - 0.5) * 0.6 // horizontal drift
    };
  }

  function draw() {
    if (!enabled || !ctx) return;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.fillStyle = 'rgba(255,255,255,0.9)';
    ctx.beginPath();
    for (let i = 0; i < flakes.length; i++) {
      const f = flakes[i];
      ctx.moveTo(f.x, f.y);
      ctx.arc(f.x, f.y, f.r, 0, Math.PI * 2);
    }
    ctx.fill();
    update();
    animationId = requestAnimationFrame(draw);
  }

  function update() {
    for (let i = 0; i < flakes.length; i++) {
      const f = flakes[i];
      f.y += f.d;
      f.x += f.w + Math.sin(f.y * 0.01) * 0.2; // gentle sway
      if (f.y > canvas.height + 5 || f.x < -5 || f.x > canvas.width + 5) {
        flakes[i] = makeFlake(false);
      }
    }
  }

  function enableSnow() {
    if (enabled) return;
    enabled = true;
    if (!canvas) {
      createCanvas();
    }
    initFlakes();
    cancelAnimationFrame(animationId);
    animationId = requestAnimationFrame(draw);
  }

  function disableSnow() {
    enabled = false;
    cancelAnimationFrame(animationId);
    animationId = null;
    if (ctx) ctx.clearRect(0, 0, canvas.width, canvas.height);
  }

  // Auto-enable on DOM ready
  document.addEventListener('DOMContentLoaded', function() {
    enableSnow();
  });

  // Expose controls
  window.enableSnow = enableSnow;
  window.disableSnow = disableSnow;
})();