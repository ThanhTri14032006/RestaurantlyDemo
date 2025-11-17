<style>
    /* Theme */
    .home-hero{
        background:linear-gradient(rgba(12,11,9,.65),rgba(12,11,9,.65)),url('https://images.unsplash.com/photo-1470337458703-46ad1756a187?q=80&w=1600&auto=format&fit=crop') center/cover;
        padding-top: 40px; /* adjust top spacing from header to hero content */
        padding-bottom: 60px;
    }
    .home-quote,.home-story,.home-specials{background:#120f0c;}
    .home-banner{background:linear-gradient(rgba(12,11,9,.65),rgba(12,11,9,.65)),url('https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1600&auto=format&fit=crop') center/cover; padding:100px 0; position:relative;}

    /* Accent */
    .text-highlight{color:#e6a15d}
    .text-accent{color:#e6a15d}
    .btn-accent{background:#e6a15d;color:#120f0c;border:none}
    .btn-outline-accent{border-color:#e6a15d;color:#e6a15d}
    .btn-outline-accent:hover{background:#e6a15d;color:#120f0c}

    /* Hero media */
    .hero-media{position:relative;border-radius:12px;overflow:hidden}
    .hero-media img{width:100%;height:360px;object-fit:cover;display:block}
    .play-badge{position:absolute;top:12px;left:12px;background:#e6a15d;color:#120f0c;width:44px;height:44px;border-radius:50%;display:flex;align-items:center;justify-content:center}

    /* Quote */
    .quote-mark{font-size:48px;color:#e6a15d;margin-bottom:12px}
    .quote-author{text-transform:uppercase;letter-spacing:.1em;color:#e6a15d;margin-top:8px}
    .quote-divider{color:#e6a15d}

    /* Story visuals */
    .story-bg{position:absolute;top:-20px;left:0;width:85%;height:70%;background:url('https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1200&auto=format&fit=crop') center/cover;border-radius:6px;opacity:.35}
    .story-plate img{width:100%;height:360px;object-fit:cover;border-radius:8px}
    .section-eyebrow{color:#e6a15d;letter-spacing:.15em;margin-bottom:.5rem}

    /* Specials */
    .special-list{list-style:none;padding:0;margin:0}
    .special-list li{padding:14px 0;border-bottom:1px solid rgba(255,255,255,.08)}
    .special-list .name{font-weight:600}
    .special-list .dots{flex:1;border-bottom:2px dotted rgba(255,255,255,.25);margin:0 10px}
    .special-list li{display:grid;grid-template-columns:auto 1fr auto;align-items:end;gap:8px}
    .special-list .price{color:#e6a15d;font-weight:700}
    .special-list .sub{grid-column:1/-1;color:#ffffff80;font-size:.95rem;margin-top:4px}
    .special-grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}
    .special-grid img{width:100%;height:220px;object-fit:cover;border-radius:8px}
    .special-grid img:first-child{grid-column:1/-1;height:320px}

    /* Banner */
    .home-banner .banner-overlay{padding:100px 0}
</style>

<style>
 .min-vh-60 {
    min-height: 60vh;
 }
  .hero-section {
      position: relative;
  }
 .hero-section:after {
     content: "";
     position: absolute;
     bottom: 0;
     left: 0;
     right: 0;
     height: 80px;
     background: linear-gradient(to bottom, rgba(0,0,0,0), rgba(0,0,0,0.25));
 }

/* Gallery hover effects */
.gallery-item {
    overflow: hidden;
    border-radius: 0.5rem;
    transition: transform 0.3s ease;
}

.gallery-item:hover {
    transform: translateY(-5px);
}

.gallery-item img {
    transition: transform 0.3s ease;
}

.gallery-item:hover img {
    transform: scale(1.05);
}

/* Card hover effects */
.card {
    transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.card:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 25px rgba(0,0,0,0.15) !important;
}

/* Animation for stats */
.stat-item h2 {
    color: #ffc107;
}

/* Fade in animation */
.fade-in {
    opacity: 0;
    transform: translateY(30px);
    transition: opacity 0.6s ease, transform 0.6s ease;
}

.fade-in.visible {
    opacity: 1;
    transform: translateY(0);
}

/* Button hover effects */
.btn {
    transition: all 0.3s ease;
}

.btn:hover {
    transform: translateY(-2px);
    box-shadow: 0 5px 15px rgba(0,0,0,0.2);
}
</style>