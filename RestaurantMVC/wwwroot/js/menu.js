document.addEventListener('DOMContentLoaded', function() {
  const menuContainer = document.getElementById('menuContainer');
  const menuItems = Array.from(document.querySelectorAll('.menu-item'));
  const filterBtns = Array.from(document.querySelectorAll('.filter-btn'));
  const searchInput = document.getElementById('menuSearch');
  const sortSelect = document.getElementById('sortSelect');
  const viewToggles = Array.from(document.querySelectorAll('.view-toggle'));
  const noResults = document.getElementById('noResults');
  let currentView = 'grid';

  animateOnScroll();

  // Initialize selected category from data attribute
  const selectedCategory = (menuContainer?.dataset?.selectedCategory || '').trim();
  const allBtn = filterBtns.find(b => b.getAttribute('data-category') === 'all');
  const matchBtn = selectedCategory ? filterBtns.find(b => b.getAttribute('data-category') === selectedCategory) : allBtn;
  if (matchBtn) {
    filterBtns.forEach(b => b.classList.remove('active'));
    matchBtn.classList.add('active');
    filterItems(selectedCategory || 'all');
  }

  // Filter functionality
  filterBtns.forEach(btn => {
    btn.addEventListener('click', function() {
      filterBtns.forEach(b => b.classList.remove('active'));
      this.classList.add('active');
      const category = this.getAttribute('data-category');
      filterItems(category);
    });
  });

  // Search functionality
  if (searchInput) {
    searchInput.addEventListener('input', function() {
      const searchTerm = this.value.toLowerCase();
      searchItems(searchTerm);
    });
  }

  // Sort functionality
  if (sortSelect) {
    sortSelect.addEventListener('change', function() {
      sortItems(this.value);
    });
  }

  // View toggle
  viewToggles.forEach(toggle => {
    toggle.addEventListener('click', function() {
      viewToggles.forEach(t => t.classList.remove('active'));
      this.classList.add('active');
      currentView = this.getAttribute('data-view');
      toggleView(currentView);
    });
  });

  function filterItems(category) {
    let visibleCount = 0;
    menuItems.forEach(item => {
      const itemCategory = item.getAttribute('data-category');
      if (category === 'all' || itemCategory === category) {
        item.style.display = 'block';
        // Bảo đảm không bị ẩn do .fade-in (opacity:0)
        item.classList.add('fade-in', 'visible');
        visibleCount++;
      } else {
        item.style.display = 'none';
        item.classList.remove('visible');
      }
    });
    toggleNoResults(visibleCount === 0);
  }

  function searchItems(searchTerm) {
    let visibleCount = 0;
    menuItems.forEach(item => {
      const itemName = item.getAttribute('data-name');
      const nameElement = item.querySelector('.card-title');
      if (!nameElement) return;
      // Remove previous highlights
      nameElement.innerHTML = nameElement.textContent;

      if (itemName.includes(searchTerm) || searchTerm === '') {
        item.style.display = 'block';
        item.classList.add('fade-in', 'visible');
        visibleCount++;
        if (searchTerm !== '') {
          const regex = new RegExp(`(${searchTerm})`, 'gi');
          nameElement.innerHTML = nameElement.textContent.replace(regex, '<span class="highlight">$1</span>');
        }
      } else {
        item.style.display = 'none';
        item.classList.remove('visible');
      }
    });
    toggleNoResults(visibleCount === 0);
  }

  function sortItems(sortBy) {
    if (!menuContainer) return;
    const itemsArray = Array.from(menuContainer.children);
    itemsArray.sort((a, b) => {
      const nameA = a.querySelector('.card-title')?.textContent?.trim()?.toLowerCase() || '';
      const nameB = b.querySelector('.card-title')?.textContent?.trim()?.toLowerCase() || '';
      const priceA = parseFloat(a.getAttribute('data-price')) || 0;
      const priceB = parseFloat(b.getAttribute('data-price')) || 0;
      switch (sortBy) {
        case 'name': return nameA.localeCompare(nameB);
        case 'price-asc': return priceA - priceB;
        case 'price-desc': return priceB - priceA;
        case 'category':
          return (a.getAttribute('data-category') || '').localeCompare(b.getAttribute('data-category') || '');
        default: return 0;
      }
    });
    itemsArray.forEach(item => menuContainer.appendChild(item));
  }

  function toggleView(view) {
    if (view === 'list') {
      menuContainer.classList.add('list-view');
      menuItems.forEach(item => {
        item.classList.remove('col-lg-4', 'col-md-6');
        item.classList.add('col-12');
      });
    } else {
      menuContainer.classList.remove('list-view');
      menuItems.forEach(item => {
        item.classList.remove('col-12');
        item.classList.add('col-lg-4', 'col-md-6');
      });
    }
  }

  function toggleNoResults(show) {
    if (!noResults) return;
    // Chỉ hiển thị thông báo, KHÔNG ẩn menuContainer để tránh “hiện xong rồi tắt”
    if (show) {
      noResults.classList.remove('d-none');
    } else {
      noResults.classList.add('d-none');
    }
    if (menuContainer) {
      // Giữ nguyên hiển thị container; Bootstrap .row sẽ là flex mặc định
      menuContainer.style.display = '';
    }
  }

  function animateOnScroll() {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
        }
      });
    }, { threshold: 0.1 });
    document.querySelectorAll('.fade-in').forEach(el => observer.observe(el));
  }
});