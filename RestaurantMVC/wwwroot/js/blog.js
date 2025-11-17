document.addEventListener('DOMContentLoaded', function () {
  const searchInput = document.getElementById('blogSearch');
  const list = document.getElementById('blogList');
  const noResults = document.getElementById('noResults');
  const prevBtn = document.getElementById('prevPage');
  const nextBtn = document.getElementById('nextPage');
  const pageInfo = document.getElementById('pageInfo');
  let cards = [];
  let filtered = [];
  let pageSize = 6;
  let currentPage = 1;
  if (searchInput && list) {
    cards = Array.from(list.querySelectorAll('.blog-card')).map(card => ({
      el: card,
      title: (card.querySelector('.card-title')?.textContent || '').toLowerCase(),
      excerpt: (card.querySelector('.card-text')?.textContent || '').toLowerCase()
    }));

    const filter = (q) => {
      const query = (q || '').trim().toLowerCase();
      const hasQuery = query.length >= 2;
      filtered = cards.filter(c => !hasQuery || c.title.includes(query) || c.excerpt.includes(query));
      currentPage = 1;
      render();
    };

    searchInput.addEventListener('input', (e) => filter(e.target.value));
  }

  // Tag buttons quick filter
  const tagButtons = document.querySelectorAll('.blog-panel .tag');
  tagButtons.forEach(btn => btn.addEventListener('click', () => {
    if (searchInput) {
      searchInput.value = btn.dataset.query || '';
      const event = new Event('input');
      searchInput.dispatchEvent(event);
      searchInput.focus();
    }
  }));

  // Newsletter subscribe (client-side mock)
  const subBtn = document.getElementById('btnSubscribe');
  const subEmail = document.getElementById('newsletterEmail');
  if (subBtn && subEmail) {
    subBtn.addEventListener('click', () => {
      const email = (subEmail.value || '').trim();
      if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        alert('Vui lòng nhập email hợp lệ');
        return;
      }
      try { if (typeof showNotification === 'function') showNotification('Đăng ký nhận tin thành công', 'success'); } catch {}
      alert('Đăng ký nhận tin thành công.');
      subEmail.value = '';
    });
  }

  // Pagination
  function render() {
    const data = filtered && filtered.length ? filtered : cards;
    const total = data.length;
    const totalPages = Math.max(1, Math.ceil(total / pageSize));
    currentPage = Math.min(currentPage, totalPages);

    if (noResults) noResults.classList.toggle('d-none', total > 0);

    // Hide all
    cards.forEach(c => c.el.closest('.col-md-6').style.display = 'none');

    // Show current page
    const start = (currentPage - 1) * pageSize;
    const pageItems = data.slice(start, start + pageSize);
    pageItems.forEach(c => c.el.closest('.col-md-6').style.display = '');

    if (pageInfo) pageInfo.textContent = `${totalPages === 0 ? 0 : currentPage}/${totalPages}`;
    if (prevBtn) prevBtn.parentElement.classList.toggle('disabled', currentPage <= 1);
    if (nextBtn) nextBtn.parentElement.classList.toggle('disabled', currentPage >= totalPages);
  }

  if (prevBtn) prevBtn.addEventListener('click', () => { if (currentPage > 1) { currentPage--; render(); } });
  if (nextBtn) nextBtn.addEventListener('click', () => {
    const data = filtered && filtered.length ? filtered : cards;
    const totalPages = Math.max(1, Math.ceil(data.length / pageSize));
    if (currentPage < totalPages) { currentPage++; render(); }
  });

  // Initial render
  setTimeout(render, 0);
});

function shareOnFacebook(url) {
  const shareUrl = 'https://www.facebook.com/sharer/sharer.php?u=' + encodeURIComponent(url);
  window.open(shareUrl, '_blank', 'noopener,noreferrer');
}

function copyLink(url) {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(url).then(() => {
      try { if (typeof showNotification === 'function') showNotification('Đã sao chép link', 'success'); } catch {}
      alert('Đã sao chép link bài viết.');
    }).catch(() => alert('Không thể sao chép link.'));
  } else {
    alert('Trình duyệt không hỗ trợ sao chép link tự động.');
  }
}