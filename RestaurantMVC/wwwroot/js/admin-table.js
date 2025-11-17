document.addEventListener('DOMContentLoaded', () => {
  const tables = document.querySelectorAll('table[data-enhance="true"]');
  tables.forEach(initEnhancedTable);
});

function initEnhancedTable(table) {
  const pageSize = parseInt(table.getAttribute('data-page-size') || '10', 10);
  const tbody = table.querySelector('tbody');
  if (!tbody) return;

  const rows = Array.from(tbody.querySelectorAll('tr'));
  let currentPage = 1;
  let currentSort = { index: null, asc: true };

  // Build pagination UI
  const pager = createPager();
  table.parentElement.appendChild(pager.container);

  // Bind header sort
  const headers = table.querySelectorAll('thead th');
  headers.forEach((th, idx) => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
      if (currentSort.index === idx) {
        currentSort.asc = !currentSort.asc;
      } else {
        currentSort.index = idx;
        currentSort.asc = true;
      }
      sortRows(rows, idx, currentSort.asc);
      renderPage();
      updateSortIndicators(headers, idx, currentSort.asc);
    });
  });

  function renderPage() {
    const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
    currentPage = Math.min(currentPage, totalPages);
    tbody.innerHTML = '';
    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    rows.slice(start, end).forEach(r => tbody.appendChild(r));
    pager.update(currentPage, totalPages);
  }

  function createPager() {
    const container = document.createElement('div');
    container.className = 'd-flex justify-content-between align-items-center mt-3';

    const info = document.createElement('div');
    info.className = 'text-muted small';

    const controls = document.createElement('div');
    controls.className = 'btn-group';

    const prev = document.createElement('button');
    prev.className = 'btn btn-outline-secondary btn-sm';
    prev.innerHTML = '<i class="fas fa-chevron-left"></i>';
    prev.addEventListener('click', () => {
      if (currentPage > 1) {
        currentPage--;
        renderPage();
      }
    });

    const next = document.createElement('button');
    next.className = 'btn btn-outline-secondary btn-sm';
    next.innerHTML = '<i class="fas fa-chevron-right"></i>';
    next.addEventListener('click', () => {
      const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
      if (currentPage < totalPages) {
        currentPage++;
        renderPage();
      }
    });

    controls.appendChild(prev);
    controls.appendChild(next);
    container.appendChild(info);
    container.appendChild(controls);

    return {
      container,
      update: (page, total) => {
        info.textContent = `Trang ${page} / ${total}`;
        prev.disabled = page <= 1;
        next.disabled = page >= total;
      }
    };
  }

  function updateSortIndicators(headers, activeIdx, asc) {
    headers.forEach((h, i) => {
      h.classList.remove('text-primary');
      h.innerHTML = h.innerText; // reset content to text only
      if (i === activeIdx) {
        h.classList.add('text-primary');
        const icon = asc ? '<i class="fas fa-sort-up ms-1"></i>' : '<i class="fas fa-sort-down ms-1"></i>';
        h.innerHTML = h.innerText + icon;
      }
    });
  }

  function sortRows(rows, colIdx, asc) {
    rows.sort((a, b) => {
      const av = getCellValue(a, colIdx);
      const bv = getCellValue(b, colIdx);
      const [aN, bN] = [parseNumber(av), parseNumber(bv)];
      let cmp;
      if (!isNaN(aN) && !isNaN(bN)) {
        cmp = aN - bN;
      } else {
        const [aD, bD] = [parseDate(av), parseDate(bv)];
        if (aD && bD) {
          cmp = aD - bD;
        } else {
          cmp = av.localeCompare(bv, 'vi', { numeric: true });
        }
      }
      return asc ? cmp : -cmp;
    });
  }

  function getCellValue(row, idx) {
    const cell = row.children[idx];
    return (cell?.innerText || '').trim();
  }

  function parseNumber(text) {
    const cleaned = text.replace(/[^0-9.,-]/g, '').replace(/\./g, '').replace(/,/g, '.');
    return parseFloat(cleaned);
  }

  function parseDate(text) {
    // Try dd/MM/yyyy HH:mm or dd/MM/yyyy
    const match = text.match(/(\d{2})\/(\d{2})\/(\d{4})(?:\s+(\d{2}):(\d{2}))?/);
    if (!match) return null;
    const [_, d, m, y, hh='00', mm='00'] = match;
    return new Date(parseInt(y,10), parseInt(m,10)-1, parseInt(d,10), parseInt(hh,10), parseInt(mm,10));
  }

  // initial render
  renderPage();
}