// Restaurant MVC - Enhanced JavaScript Features
// Performance optimizations and new features

document.addEventListener('DOMContentLoaded', function() {
    // Initialize all features
    initLazyLoading();
    initSearchFunctionality();
    initRatingSystem();
    initPerformanceOptimizations();
    initBootstrapDropdowns();
});

// Lazy Loading Images for Performance
function initLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');
    
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy-image');
                    img.classList.add('loaded');
                    imageObserver.unobserve(img);
                }
            });
        });

        images.forEach(img => {
            img.classList.add('lazy-image');
            imageObserver.observe(img);
        });
    } else {
        // Fallback for older browsers
        images.forEach(img => {
            img.src = img.dataset.src;
            img.classList.add('loaded');
        });
    }
}

// Real-time Search Functionality
function initSearchFunctionality() {
    const searchInput = document.getElementById('menuSearch');
    if (!searchInput) return;

    let searchTimeout;
    const searchResults = document.createElement('div');
    searchResults.className = 'search-results';
    searchInput.parentNode.appendChild(searchResults);

    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const query = this.value.trim();

        if (query.length < 2) {
            searchResults.style.display = 'none';
            return;
        }

        searchTimeout = setTimeout(() => {
            performSearch(query, searchResults);
        }, 300);
    });

    // Hide search results when clicking outside
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });
}

// AJAX Search Function
function performSearch(query, resultsContainer) {
    const loadingSpinner = '<div class="text-center p-3"><div class="loading-spinner"></div></div>';
    resultsContainer.innerHTML = loadingSpinner;
    resultsContainer.style.display = 'block';

    fetch(`/Menu/Search?query=${encodeURIComponent(query)}`)
        .then(response => response.json())
        .then(data => {
            displaySearchResults(data, resultsContainer);
        })
        .catch(error => {
            console.error('Search error:', error);
            resultsContainer.innerHTML = '<div class="search-result-item text-danger">Lỗi tìm kiếm</div>';
        });
}

// Display Search Results
function displaySearchResults(items, container) {
    if (items.length === 0) {
        container.innerHTML = '<div class="search-result-item text-muted">Không tìm thấy món ăn nào</div>';
        return;
    }

    const placeholder = '/img/backgroudcheck.png';
    const html = items.map(item => `
        <div class="search-result-item" onclick="window.location.href='/Menu/Details/${item.id}'">
            <div class="d-flex align-items-center">
                <img src="${item.imageUrl || placeholder}" 
                     alt="${item.name}" 
                     style="width: 40px; height: 40px; object-fit: cover; border-radius: 4px; margin-right: 12px;" onerror="this.onerror=null; this.src='${placeholder}';">
                <div>
                    <div class="fw-bold">${item.name}</div>
                    <div class="text-muted small">${formatPrice(item.price)} - ${item.category}</div>
                </div>
            </div>
        </div>
    `).join('');

    container.innerHTML = html;
}

// Rating System
function initRatingSystem() {
    const ratingContainers = document.querySelectorAll('.rating-input');
    
    ratingContainers.forEach(container => {
        const stars = container.querySelectorAll('label');
        const inputs = container.querySelectorAll('input[type="radio"]');
        
        stars.forEach((star, index) => {
            star.addEventListener('mouseenter', () => {
                highlightStars(stars, index + 1);
            });
            
            star.addEventListener('click', () => {
                inputs[index].checked = true;
                setActiveStars(stars, index + 1);
            });
        });
        
        container.addEventListener('mouseleave', () => {
            const checkedInput = container.querySelector('input[type="radio"]:checked');
            const activeRating = checkedInput ? parseInt(checkedInput.value) : 0;
            setActiveStars(stars, activeRating);
        });
    });
}

function highlightStars(stars, rating) {
    stars.forEach((star, index) => {
        if (index < rating) {
            star.classList.add('active');
        } else {
            star.classList.remove('active');
        }
    });
}

function setActiveStars(stars, rating) {
    stars.forEach((star, index) => {
        if (index < rating) {
            star.classList.add('active');
        } else {
            star.classList.remove('active');
        }
    });
}

// Performance Optimizations
function initPerformanceOptimizations() {
    // Debounce scroll events
    let scrollTimeout;
    window.addEventListener('scroll', function() {
        clearTimeout(scrollTimeout);
        scrollTimeout = setTimeout(handleScroll, 10);
    });

    // Optimize animations
    const animatedElements = document.querySelectorAll('.fade-in');
    if ('IntersectionObserver' in window) {
        const animationObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.animationPlayState = 'running';
                }
            });
        });

        animatedElements.forEach(el => {
            el.style.animationPlayState = 'paused';
            animationObserver.observe(el);
        });
    }

    // Preload critical resources
    preloadCriticalResources();
}

function handleScroll() {
    // Add scroll-based optimizations here
    const scrollTop = window.pageYOffset;
    
    // Example: Parallax effect for hero section
    const heroSection = document.querySelector('.hero-section');
    if (heroSection) {
        const scrolled = scrollTop * 0.5;
        heroSection.style.transform = `translateY(${scrolled}px)`;
    }
}

function preloadCriticalResources() {
    // Preload important images
    const criticalImages = [
        '/images/hero-bg.jpg',
        '/images/logo.png'
    ];

    criticalImages.forEach(src => {
        const link = document.createElement('link');
        link.rel = 'preload';
        link.as = 'image';
        link.href = src;
        document.head.appendChild(link);
    });
}

// Ensure Bootstrap 5 dropdowns are initialized (fallback in case auto-init is disrupted)
function initBootstrapDropdowns() {
    try {
        if (typeof bootstrap !== 'undefined' && bootstrap.Dropdown) {
            document.querySelectorAll('.dropdown-toggle').forEach(function (el) {
                // Avoid duplicate init by storing a flag
                if (!el._bsDropdownInstance) {
                    el._bsDropdownInstance = new bootstrap.Dropdown(el);
                }
            });
        }
    } catch (e) {
        console.error('Dropdown init error:', e);
    }
}

// Utility Functions
function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(price);
}

// AJAX Form Submission with Loading States
function submitFormWithLoading(form, successCallback) {
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    
    submitBtn.innerHTML = '<div class="loading-spinner"></div> Đang xử lý...';
    submitBtn.disabled = true;

    const formData = new FormData(form);
    
    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            successCallback(data);
        } else {
            showNotification('Có lỗi xảy ra: ' + data.message, 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Có lỗi xảy ra khi gửi dữ liệu', 'error');
    })
    .finally(() => {
        submitBtn.innerHTML = originalText;
        submitBtn.disabled = false;
    });
}

// Notification System
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : 'success'} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 5000);
}
