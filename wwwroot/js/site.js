// Natalya Parapharmacie Frontend Engine

document.addEventListener('DOMContentLoaded', () => {
    // Initialize Hero Carousel Slider
    initHeroSlider();

    // Mobile Navigation Drawer Setup
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const closeDrawerBtn = document.getElementById('closeDrawerBtn');
    const mobileDrawer = document.getElementById('mobileDrawer');

    if (mobileMenuBtn && mobileDrawer) {
        mobileMenuBtn.addEventListener('click', () => {
            mobileDrawer.classList.remove('hidden');
        });
    }

    if (closeDrawerBtn && mobileDrawer) {
        closeDrawerBtn.addEventListener('click', () => {
            mobileDrawer.classList.add('hidden');
        });
    }

    if (mobileDrawer) {
        mobileDrawer.addEventListener('click', (e) => {
            if (e.target === mobileDrawer) {
                mobileDrawer.classList.add('hidden');
            }
        });
    }

    // Keyboard listener (Escape to close drawers)
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            closeCartDrawer();
            if (mobileDrawer) mobileDrawer.classList.add('hidden');
        }
    });

    // Product Details Quantity Controls
    const btnDecreaseQty = document.getElementById('btnDecreaseQty');
    const btnIncreaseQty = document.getElementById('btnIncreaseQty');
    const detailQty = document.getElementById('detailQty');

    if (btnDecreaseQty && detailQty) {
        btnDecreaseQty.addEventListener('click', () => {
            let val = parseInt(detailQty.value) || 1;
            if (val > 1) {
                detailQty.value = val - 1;
            }
        });
    }

    if (btnIncreaseQty && detailQty) {
        btnIncreaseQty.addEventListener('click', () => {
            let val = parseInt(detailQty.value) || 1;
            detailQty.value = val + 1;
        });
    }

    // Auto-play all reel videos on the page
    initReelVideos();

    // Initialize Mobile Promo Carousel Dots
    initPromoCarousel();

    // Initialize Category Scroll Bar (Swipe, Drag, Wheel)
    initCategoryScroll();
});

// Category Scroll Bar Engine (Touch Swipe, Mouse Drag, Wheel Translation, Arrows)
function initCategoryScroll() {
    const container = document.getElementById('categoryScrollContainer');
    if (!container) return;

    // Mouse wheel horizontal translation (vertical wheel scrolls horizontally)
    container.addEventListener('wheel', (e) => {
        if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
            e.preventDefault();
            container.scrollLeft += e.deltaY * 0.9;
        }
    }, { passive: false });

    // Click & Drag to Scroll for Desktop & Laptop users
    let isDown = false;
    let startX = 0;
    let scrollLeft = 0;
    let moved = false;

    container.addEventListener('mousedown', (e) => {
        isDown = true;
        moved = false;
        container.classList.add('cursor-grabbing');
        startX = e.pageX - container.offsetLeft;
        scrollLeft = container.scrollLeft;
    });

    window.addEventListener('mouseup', () => {
        if (isDown) {
            isDown = false;
            container.classList.remove('cursor-grabbing');
        }
    });

    container.addEventListener('mousemove', (e) => {
        if (!isDown) return;
        e.preventDefault();
        const x = e.pageX - container.offsetLeft;
        const walk = (x - startX) * 1.5;
        if (Math.abs(walk) > 5) {
            moved = true;
        }
        container.scrollLeft = scrollLeft - walk;
    });

    // Prevent accidental link click if the user was dragging to scroll
    container.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', (e) => {
            if (moved) {
                e.preventDefault();
                e.stopPropagation();
                moved = false;
            }
        });
    });
}

function scrollCatBar(distance) {
    const container = document.getElementById('categoryScrollContainer');
    if (container) {
        container.scrollBy({ left: distance, behavior: 'smooth' });
    }
}

// Mobile Promo Carousel Dots Sync
function initPromoCarousel() {
    const track = document.getElementById('promoCarouselTrack');
    const dotsContainer = document.getElementById('promoCarouselDots');
    if (!track || !dotsContainer) return;

    const dots = dotsContainer.querySelectorAll('.promo-dot');
    if (!dots.length) return;

    track.addEventListener('scroll', () => {
        const scrollLeft = track.scrollLeft;
        const cardWidth = track.firstElementChild ? track.firstElementChild.offsetWidth + 16 : 300;
        const activeIndex = Math.round(scrollLeft / cardWidth);

        dots.forEach((dot, index) => {
            if (index === activeIndex) {
                dot.classList.remove('w-2', 'bg-slate-300');
                dot.classList.add('w-6', 'bg-[#15803D]');
            } else {
                dot.classList.remove('w-6', 'bg-[#15803D]');
                dot.classList.add('w-2', 'bg-slate-300');
            }
        });
    }, { passive: true });
}

// Reel Video Controller Engine
function initReelVideos() {
    const reelVideos = document.querySelectorAll('.reel-video');
    reelVideos.forEach(video => {
        video.muted = true;
        video.setAttribute('playsinline', '');
        video.setAttribute('muted', '');
        video.setAttribute('autoplay', '');
        const playPromise = video.play();
        if (playPromise !== undefined) {
            playPromise.catch(() => {
                // Autoplay was prevented, will play on scroll or first user click
            });
        }
    });
}

function toggleReelVideo(btn) {
    const card = btn.closest('.reel-card');
    if (!card) return;
    const video = card.querySelector('video');
    const icon = btn.querySelector('.reel-play-icon');
    if (!video) return;

    if (video.paused) {
        video.play();
        if (icon) icon.textContent = 'pause';
        showToast('Vidéo en lecture');
    } else {
        video.pause();
        if (icon) icon.textContent = 'play_arrow';
        showToast('Vidéo en pause');
    }
}

function toggleReelSound(btn) {
    const card = btn.closest('.reel-card');
    if (!card) return;
    const video = card.querySelector('video');
    const icon = btn.querySelector('.reel-sound-icon');
    if (!video) return;

    video.muted = !video.muted;
    if (icon) {
        icon.textContent = video.muted ? 'volume_off' : 'volume_up';
    }
    showToast(video.muted ? 'Son désactivé' : 'Son activé');
}

// Auto-run story reels immediately like Facebook / Instagram stories with infinite repeating loop
function startReelStories() {
    document.querySelectorAll('.reel-video').forEach(video => {
        video.muted = true;
        video.playsInline = true;
        video.loop = true;
        video.setAttribute('loop', 'true');
        
        // Force restart on ended to prevent any browser freezing at the end
        video.onended = function () {
            this.currentTime = 0;
            const retry = this.play();
            if (retry !== undefined) retry.catch(() => {});
        };

        video.addEventListener('ended', function () {
            this.currentTime = 0;
            const retry = this.play();
            if (retry !== undefined) retry.catch(() => {});
        });

        const p = video.play();
        if (p !== undefined) {
            p.catch(() => {});
        }
    });
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', startReelStories);
} else {
    startReelStories();
}
window.addEventListener('load', startReelStories);

// Hero Section Modern Parisian Luxury Carousel Engine
function initHeroSlider() {
    const slider = document.getElementById('heroSlider');
    if (!slider) return;

    const slides = slider.querySelectorAll('.hero-slide');
    const tabs = slider.querySelectorAll('.hero-tab');
    const dots = slider.querySelectorAll('.hero-dot');
    const prevBtn = document.getElementById('heroPrevBtn');
    const nextBtn = document.getElementById('heroNextBtn');
    const slideNum = document.getElementById('heroSlideNum');

    if (!slides.length) return;

    let currentIndex = 0;
    let autoplayTimer = null;
    let progressAnimFrame = null;
    let progressStartTime = null;
    const intervalDuration = 5500;

    function resetAllProgressBars() {
        if (progressAnimFrame) {
            cancelAnimationFrame(progressAnimFrame);
            progressAnimFrame = null;
        }
        slider.querySelectorAll('.hero-tab-progress').forEach(bar => {
            bar.style.width = '0%';
        });
    }

    function animateTabProgress() {
        resetAllProgressBars();
        const activeTab = tabs[currentIndex];
        if (!activeTab) return;
        const progressBar = activeTab.querySelector('.hero-tab-progress');
        if (!progressBar) return;

        progressStartTime = performance.now();

        function step(now) {
            const elapsed = now - progressStartTime;
            const progress = Math.min(elapsed / intervalDuration, 1);
            progressBar.style.width = `${progress * 100}%`;

            if (progress < 1) {
                progressAnimFrame = requestAnimationFrame(step);
            }
        }

        progressAnimFrame = requestAnimationFrame(step);
    }

    function goToSlide(index) {
        if (index < 0) index = slides.length - 1;
        if (index >= slides.length) index = 0;

        currentIndex = index;

        // Animate Slide Visibility & Content
        slides.forEach((slide, i) => {
            const contentElements = slide.querySelectorAll('.slide-content > div, .slide-content > h1, .slide-content > p');
            const img = slide.querySelector('.hero-slide-img');

            if (i === currentIndex) {
                slide.classList.remove('opacity-0', 'pointer-events-none', 'z-0');
                slide.classList.add('opacity-100', 'pointer-events-auto', 'z-10');

                if (img) {
                    img.classList.remove('scale-100');
                    img.classList.add('scale-105');
                }

                contentElements.forEach(el => {
                    el.classList.remove('translate-y-4', 'opacity-0');
                    el.classList.add('translate-y-0', 'opacity-100');
                });
            } else {
                slide.classList.remove('opacity-100', 'pointer-events-auto', 'z-10');
                slide.classList.add('opacity-0', 'pointer-events-none', 'z-0');

                if (img) {
                    img.classList.remove('scale-105');
                    img.classList.add('scale-100');
                }

                contentElements.forEach(el => {
                    el.classList.add('translate-y-4', 'opacity-0');
                    el.classList.remove('translate-y-0', 'opacity-100');
                });
            }
        });

        // Update Interactive Category Tabs
        tabs.forEach((tab, i) => {
            const tabNum = tab.querySelector('.tab-num');
            const label = tab.querySelector('span:last-child');
            if (i === currentIndex) {
                tab.classList.add('active', 'bg-emerald-50');
                if (tabNum) {
                    tabNum.classList.remove('text-slate-400');
                    tabNum.classList.add('text-[#15803D]');
                }
                if (label) {
                    label.classList.remove('text-slate-600');
                    label.classList.add('text-[#15803D]');
                }
            } else {
                tab.classList.remove('active', 'bg-emerald-50');
                if (tabNum) {
                    tabNum.classList.add('text-slate-400');
                    tabNum.classList.remove('text-[#15803D]');
                }
                if (label) {
                    label.classList.add('text-slate-600');
                    label.classList.remove('text-[#15803D]');
                }
            }
        });

        // Update Minimalist Dots with Theme Colors
        const themeDotColors = ['bg-amber-600', 'bg-blue-600', 'bg-rose-600'];
        dots.forEach((dot, i) => {
            if (i === currentIndex) {
                dot.classList.remove('w-2', 'w-2.5', 'bg-slate-300', 'bg-slate-200', 'bg-amber-600', 'bg-blue-600', 'bg-rose-600', 'bg-[#15803D]');
                dot.classList.add('w-7', themeDotColors[i] || 'bg-slate-900');
            } else {
                dot.classList.remove('w-7', 'w-8', 'bg-amber-600', 'bg-blue-600', 'bg-rose-600', 'bg-[#15803D]', 'bg-slate-900');
                dot.classList.add('w-2.5', 'bg-slate-300');
            }
        });

        // Update Counter
        if (slideNum) {
            slideNum.textContent = String(currentIndex + 1).padStart(2, '0');
        }

        // Start progress bar animation for active tab if present
        animateTabProgress();
    }

    function startAutoplay() {
        stopAutoplay();
        animateTabProgress();
        autoplayTimer = setInterval(() => {
            goToSlide(currentIndex + 1);
        }, intervalDuration);
    }

    function stopAutoplay() {
        if (autoplayTimer) {
            clearInterval(autoplayTimer);
            autoplayTimer = null;
        }
        if (progressAnimFrame) {
            cancelAnimationFrame(progressAnimFrame);
            progressAnimFrame = null;
        }
    }

    // Dot navigation clicks
    dots.forEach((dot, idx) => {
        dot.addEventListener('click', (e) => {
            e.preventDefault();
            goToSlide(idx);
            startAutoplay();
        });
    });

    // Interactive Tab Clicks (if tabs exist)
    tabs.forEach((tab, idx) => {
        tab.addEventListener('click', (e) => {
            e.preventDefault();
            goToSlide(idx);
            startAutoplay();
        });
    });

    // Arrow Controls
    if (prevBtn) {
        prevBtn.addEventListener('click', (e) => {
            e.preventDefault();
            goToSlide(currentIndex - 1);
            startAutoplay();
        });
    }

    if (nextBtn) {
        nextBtn.addEventListener('click', (e) => {
            e.preventDefault();
            goToSlide(currentIndex + 1);
            startAutoplay();
        });
    }

    // Pause on Hover
    slider.addEventListener('mouseenter', stopAutoplay);
    slider.addEventListener('mouseleave', startAutoplay);

    // Touch Swipe Support
    let touchStartX = 0;
    let touchEndX = 0;

    slider.addEventListener('touchstart', (e) => {
        touchStartX = e.changedTouches[0].screenX;
        stopAutoplay();
    }, { passive: true });

    slider.addEventListener('touchend', (e) => {
        touchEndX = e.changedTouches[0].screenX;
        if (touchStartX - touchEndX > 50) {
            goToSlide(currentIndex + 1);
        } else if (touchEndX - touchStartX > 50) {
            goToSlide(currentIndex - 1);
        }
        startAutoplay();
    }, { passive: true });

    // Initialize first slide and timer
    goToSlide(0);
    startAutoplay();
}

// Side Cart Drawer Functions
function openCartDrawer() {
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartDrawerBackdrop');
    const panel = document.getElementById('cartDrawerPanel');

    if (!drawer || !backdrop || !panel) return;

    drawer.classList.remove('pointer-events-none');
    drawer.classList.add('pointer-events-auto');

    backdrop.classList.remove('pointer-events-none', 'opacity-0');
    backdrop.classList.add('opacity-100', 'pointer-events-auto');

    panel.classList.remove('translate-x-full');
    panel.classList.add('translate-x-0');

    document.body.classList.add('overflow-hidden');

    showDrawerCart();
    fetchCartDrawer();
}

function openCheckoutDrawer() {
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartDrawerBackdrop');
    const panel = document.getElementById('cartDrawerPanel');

    if (!drawer || !backdrop || !panel) return;

    drawer.classList.remove('pointer-events-none');
    drawer.classList.add('pointer-events-auto');

    backdrop.classList.remove('pointer-events-none', 'opacity-0');
    backdrop.classList.add('opacity-100', 'pointer-events-auto');

    panel.classList.remove('translate-x-full');
    panel.classList.add('translate-x-0');

    document.body.classList.add('overflow-hidden');

    fetchCartDrawer();
    showDrawerCheckout();
}

function closeCartDrawer() {
    const drawer = document.getElementById('cartDrawer');
    const backdrop = document.getElementById('cartDrawerBackdrop');
    const panel = document.getElementById('cartDrawerPanel');

    if (!drawer || !backdrop || !panel) return;

    backdrop.classList.remove('opacity-100', 'pointer-events-auto');
    backdrop.classList.add('opacity-0', 'pointer-events-none');

    panel.classList.remove('translate-x-0');
    panel.classList.add('translate-x-full');

    document.body.classList.remove('overflow-hidden');

    setTimeout(() => {
        drawer.classList.remove('pointer-events-auto');
        drawer.classList.add('pointer-events-none');
        showDrawerCart();
    }, 300);
}

// Drawer Step Switching (Cart vs Checkout)
function showDrawerCheckout() {
    const cartStep = document.getElementById('drawerCartStep');
    const checkoutStep = document.getElementById('drawerCheckoutStep');
    const cartActions = document.getElementById('drawerCartActions');
    const checkoutActions = document.getElementById('drawerCheckoutActions');
    const summaryDetailed = document.getElementById('drawerSummaryDetailed');
    const summarySlim = document.getElementById('drawerSummarySlim');
    const progressBox = document.getElementById('drawerShippingProgressBox');
    const backBtn = document.getElementById('drawerBackBtn');
    const title = document.getElementById('cartDrawerTitle');
    const subtitle = document.getElementById('drawerItemsCountText');
    const headerIcon = document.getElementById('drawerHeaderIcon');

    if (cartStep) cartStep.classList.add('hidden');
    if (checkoutStep) checkoutStep.classList.remove('hidden');

    if (cartActions) cartActions.classList.add('hidden');
    if (checkoutActions) checkoutActions.classList.remove('hidden');

    if (summaryDetailed) summaryDetailed.classList.add('hidden');
    if (summarySlim) summarySlim.classList.remove('hidden');

    if (progressBox) progressBox.classList.add('hidden');
    if (backBtn) backBtn.classList.remove('hidden');

    if (title) title.textContent = 'Validation Rapide';
    if (subtitle) subtitle.textContent = 'Paiement à la livraison';
    if (headerIcon) headerIcon.innerHTML = '<span class="material-symbols-outlined text-base">payments</span>';
}

function showDrawerCart() {
    const cartStep = document.getElementById('drawerCartStep');
    const checkoutStep = document.getElementById('drawerCheckoutStep');
    const cartActions = document.getElementById('drawerCartActions');
    const checkoutActions = document.getElementById('drawerCheckoutActions');
    const summaryDetailed = document.getElementById('drawerSummaryDetailed');
    const summarySlim = document.getElementById('drawerSummarySlim');
    const progressBox = document.getElementById('drawerShippingProgressBox');
    const backBtn = document.getElementById('drawerBackBtn');
    const title = document.getElementById('cartDrawerTitle');
    const subtitle = document.getElementById('drawerItemsCountText');
    const headerIcon = document.getElementById('drawerHeaderIcon');

    if (cartStep) cartStep.classList.remove('hidden');
    if (checkoutStep) checkoutStep.classList.add('hidden');

    if (cartActions) cartActions.classList.remove('hidden');
    if (checkoutActions) checkoutActions.classList.add('hidden');

    if (summaryDetailed) summaryDetailed.classList.remove('hidden');
    if (summarySlim) summarySlim.classList.add('hidden');

    if (progressBox) progressBox.classList.remove('hidden');
    if (backBtn) backBtn.classList.add('hidden');

    if (title) title.textContent = 'Mon Panier';
    if (headerIcon) headerIcon.innerHTML = '<span class="material-symbols-outlined text-base">shopping_bag</span>';
}

// Cash on Delivery Checkout Submission with Full Field Validation
function submitDrawerCheckout(event) {
    if (event) event.preventDefault();

    const fullNameInput = document.getElementById('chkFullName');
    const phoneInput = document.getElementById('chkPhone');
    const cityInput = document.getElementById('chkCity');
    const addressInput = document.getElementById('chkAddress');

    let isValid = true;
    let firstInvalidInput = null;

    // Helper to mark input error
    function checkField(input, minLen, errorMsg) {
        if (!input) return true;
        const val = input.value ? input.value.trim() : '';
        if (val.length < minLen) {
            input.classList.add('border-red-500', 'bg-red-50/20', 'ring-2', 'ring-red-200');
            input.classList.remove('border-slate-200');
            if (!firstInvalidInput) firstInvalidInput = input;
            isValid = false;

            // Remove red border on typing
            input.addEventListener('input', function onInputClear() {
                if (this.value && this.value.trim().length >= minLen) {
                    this.classList.remove('border-red-500', 'bg-red-50/20', 'ring-2', 'ring-red-200');
                    this.classList.add('border-slate-200');
                    this.removeEventListener('input', onInputClear);
                }
            });
            return false;
        } else {
            input.classList.remove('border-red-500', 'bg-red-50/20', 'ring-2', 'ring-red-200');
            input.classList.add('border-slate-200');
            return true;
        }
    }

    checkField(fullNameInput, 2, 'Nom & Prénom requis');
    checkField(phoneInput, 8, 'Numéro de téléphone requis');
    checkField(cityInput, 2, 'Ville requise');
    checkField(addressInput, 5, 'Adresse complète requise');

    if (!isValid) {
        if (firstInvalidInput) {
            firstInvalidInput.focus();
        }
        showToast('Veuillez renseigner tous les champs obligatoires (*) pour la livraison.');
        return;
    }

    const form = document.getElementById('drawerCheckoutForm');
    const formData = new FormData(form);

    const submitBtn = event && event.target && event.target.querySelector ? event.target.querySelector('button[type="submit"]') : null;
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.classList.add('opacity-75', 'cursor-not-allowed');
    }

    fetch('/Cart/ProcessCheckout', {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: new URLSearchParams(formData)
    })
    .then(res => res.json())
    .then(data => {
        if (data && data.success) {
            closeCartDrawer();
            window.location.href = data.redirectUrl || '/Cart/OrderConfirmation';
        } else {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.classList.remove('opacity-75', 'cursor-not-allowed');
            }
            showToast(data.message || 'Impossible de finaliser la commande. Veuillez vérifier vos informations.');
        }
    })
    .catch(() => {
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.classList.remove('opacity-75', 'cursor-not-allowed');
        }
        window.location.href = '/Cart/OrderConfirmation';
    });
}

function fetchCartDrawer() {
    fetch('/Cart/GetDrawer', {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
    .then(res => res.json())
    .then(data => {
        if (data && data.success) {
            renderCartDrawer(data);
        }
    })
    .catch(err => {
        console.error('Erreur lors du chargement du panier:', err);
    });
}

function renderCartDrawer(data) {
    updateBadge(data.totalItems);

    const countText = document.getElementById('drawerItemsCountText');
    const cartStep = document.getElementById('drawerCartStep');
    if (countText && cartStep && !cartStep.classList.contains('hidden')) {
        countText.textContent = `${data.totalItems} soin${data.totalItems > 1 ? 's' : ''}`;
    }

    const itemsList = document.getElementById('cartDrawerItemsList');
    const emptyState = document.getElementById('cartDrawerEmpty');
    const footer = document.getElementById('cartDrawerFooter');
    const progressBox = document.getElementById('drawerShippingProgressBox');
    const progressBar = document.getElementById('drawerProgressBar');
    const shippingMsg = document.getElementById('drawerShippingMsg');

    const subtotalEl = document.getElementById('drawerSubtotal');
    const shippingEl = document.getElementById('drawerShipping');
    const grandTotalEl = document.getElementById('drawerGrandTotal');

    if (!data.items || data.items.length === 0) {
        if (itemsList) itemsList.innerHTML = '';
        if (emptyState) {
            emptyState.classList.remove('hidden');
            emptyState.classList.add('flex');
        }
        if (footer) footer.classList.add('hidden');
        if (progressBox) progressBox.classList.add('hidden');
        showDrawerCart();
        return;
    }

    if (emptyState) {
        emptyState.classList.add('hidden');
        emptyState.classList.remove('flex');
    }
    if (footer) footer.classList.remove('hidden');
    if (progressBox && (!cartStep || !cartStep.classList.contains('hidden'))) {
        progressBox.classList.remove('hidden');
    }

    // Free Shipping Progress (Threshold: 49 Dinar)
    const threshold = 49.00;
    const subtotal = data.subtotal || 0;
    const progressPercent = Math.min(100, (subtotal / threshold) * 100);

    if (progressBar) {
        progressBar.style.width = `${progressPercent}%`;
    }

    if (shippingMsg) {
        if (subtotal >= threshold) {
            shippingMsg.innerHTML = `<span class="text-black font-bold flex items-center gap-1"><span class="material-symbols-outlined text-xs text-black" style="font-variation-settings: 'FILL' 1;">verified</span> Félicitations ! Livraison Gratuite offerte.</span>`;
        } else {
            const diff = (threshold - subtotal).toFixed(2).replace('.', ',');
            shippingMsg.innerHTML = `Plus que <strong class="text-black font-bold">${diff} Dinar</strong> pour la <strong>Livraison Gratuite</strong>`;
        }
    }

    // Populate Items
    if (itemsList) {
        itemsList.innerHTML = data.items.map(item => `
            <div class="flex items-center gap-3.5 py-2 group">
                <div class="w-16 h-16 rounded-xl bg-slate-50 border border-slate-100 overflow-hidden flex-shrink-0 p-1 flex items-center justify-center">
                    <img src="${item.imageUrl}" alt="${item.name}" class="w-full h-full object-cover rounded-lg" onerror="this.src='/images/placeholder.jpg'" />
                </div>
                <div class="flex-1 min-w-0">
                    <div class="flex items-center justify-between gap-1 mb-0.5">
                        <span class="text-[9px] uppercase font-bold tracking-widest text-slate-400 truncate">${item.brand || 'NATALYA'}</span>
                        ${item.rxRequired ? '<span class="text-[8px] bg-slate-100 text-slate-700 px-1 rounded font-bold uppercase">Ordonnance</span>' : ''}
                    </div>
                    <a href="/Products/Details/${item.id}" class="text-xs font-semibold text-slate-900 hover:text-black line-clamp-1 block leading-snug">
                        ${item.name}
                    </a>
                    <div class="text-xs font-bold text-slate-900 mt-1">${item.price.toFixed(2).replace('.', ',')} Dinar</div>
                    
                    <div class="flex items-center justify-between mt-2">
                        <div class="flex items-center border border-slate-200 rounded-lg bg-slate-50 h-7 px-1">
                            <button type="button" onclick="updateCartQtyDrawer(${item.id}, ${item.quantity - 1})" class="w-6 h-6 flex items-center justify-center text-slate-600 hover:text-black rounded hover:bg-white transition-colors" title="Diminuer la quantité">
                                <span class="material-symbols-outlined text-xs">remove</span>
                            </button>
                            <span class="text-xs font-bold text-slate-900 px-2 min-w-[20px] text-center">${item.quantity}</span>
                            <button type="button" onclick="updateCartQtyDrawer(${item.id}, ${item.quantity + 1})" class="w-6 h-6 flex items-center justify-center text-slate-600 hover:text-black rounded hover:bg-white transition-colors" title="Augmenter la quantité">
                                <span class="material-symbols-outlined text-xs">add</span>
                            </button>
                        </div>
                        <div class="flex items-center gap-2">
                            <span class="text-xs font-bold text-slate-900 font-editorial">${item.subtotal.toFixed(2).replace('.', ',')} Dinar</span>
                            <button type="button" onclick="removeFromCartDrawer(${item.id})" class="text-slate-400 hover:text-red-600 p-1 transition-colors rounded-full hover:bg-red-50" title="Supprimer cet article">
                                <span class="material-symbols-outlined text-sm">delete</span>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    const grandTotalSlimEl = document.getElementById('drawerGrandTotalSlim');
    const shippingNoteEl = document.getElementById('drawerShippingNote');

    if (subtotalEl) subtotalEl.textContent = `${(data.subtotal || 0).toFixed(2).replace('.', ',')} Dinar`;
    if (shippingEl) shippingEl.textContent = data.shippingFee === 0 ? 'GRATUITE' : `${data.shippingFee.toFixed(2).replace('.', ',')} Dinar`;
    if (grandTotalEl) grandTotalEl.textContent = `${(data.grandTotal || 0).toFixed(2).replace('.', ',')} Dinar`;
    if (grandTotalSlimEl) grandTotalSlimEl.textContent = `${(data.grandTotal || 0).toFixed(2).replace('.', ',')} Dinar`;
    if (shippingNoteEl) {
        shippingNoteEl.textContent = data.shippingFee === 0 ? 'Livraison Gratuite offerte • Espèces à la livraison' : 'Frais de livraison (4,99 Dinar) inclus • Espèces à la livraison';
    }
}

// Global AJAX Cart Operations with Side Drawer Trigger
function addToCart(productId, quantity = 1) {
    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: `id=${productId}&quantity=${quantity}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            renderCartDrawer(data);
            openCartDrawer();
            showToast(data.message || 'Produit ajouté à votre sélection !');
        } else {
            showToast(data.message || 'Produit non disponible.');
        }
    })
    .catch(err => {
        console.error('Erreur lors de l\'ajout au panier:', err);
    });
}

function updateCartQtyDrawer(productId, quantity) {
    if (quantity < 1) {
        removeFromCartDrawer(productId);
        return;
    }

    fetch('/Cart/Update', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: `id=${productId}&quantity=${quantity}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            renderCartDrawer(data);
            if (window.location.pathname.toLowerCase().includes('/cart')) {
                const summarySub = document.getElementById('summarySubtotal');
                const summaryTotal = document.getElementById('summaryTotal');
                const summaryShip = document.getElementById('summaryShipping');
                if (summarySub) summarySub.textContent = `${data.subtotal.toFixed(2).replace('.', ',')} Dinar`;
                if (summaryTotal) summaryTotal.textContent = `${data.grandTotal.toFixed(2).replace('.', ',')} Dinar`;
                if (summaryShip) summaryShip.textContent = data.shippingFee === 0 ? 'GRATUITE' : `${data.shippingFee.toFixed(2).replace('.', ',')} Dinar`;
            }
        }
    });
}

function removeFromCartDrawer(productId) {
    fetch('/Cart/Remove', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: `id=${productId}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            renderCartDrawer(data);
            const row = document.getElementById(`cart-row-${productId}`);
            if (row) row.remove();
        }
    });
}

// Fallbacks for full page cart views
function updateCartQty(productId, quantity) {
    updateCartQtyDrawer(productId, quantity);
}

function removeFromCart(productId) {
    removeFromCartDrawer(productId);
}

function buyNow(productId, quantity = 1) {
    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: `id=${productId}&quantity=${quantity}`
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            renderCartDrawer(data);
            openCheckoutDrawer();
        }
    });
}

function updateBadge(totalItems) {
    const cartBadge = document.getElementById('cartBadge');
    const bottomCartBadge = document.getElementById('bottomCartBadge');

    [cartBadge, bottomCartBadge].forEach(badge => {
        if (badge) {
            badge.textContent = totalItems;
            if (totalItems > 0) {
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        }
    });
}

// Toast Notification
function showToast(message) {
    const toast = document.getElementById('toast');
    const toastMessage = document.getElementById('toastMessage');

    if (!toast || !toastMessage) return;

    toastMessage.textContent = message;
    toast.classList.remove('hidden');

    setTimeout(() => {
        toast.classList.add('hidden');
    }, 3000);
}

// Product Details Tab Switching
function switchTab(tabName) {
    const tabs = ['desc', 'ingr', 'dosage'];

    tabs.forEach(t => {
        const btn = document.getElementById(`tabBtn${t.charAt(0).toUpperCase() + t.slice(1)}`);
        const content = document.getElementById(`tabContent${t.charAt(0).toUpperCase() + t.slice(1)}`);

        if (t === tabName) {
            if (btn) {
                btn.classList.add('text-primary', 'border-b-2', 'border-primary');
                btn.classList.remove('text-on-surface-variant');
            }
            if (content) {
                content.classList.remove('hidden');
            }
        } else {
            if (btn) {
                btn.classList.remove('text-primary', 'border-b-2', 'border-primary');
                btn.classList.add('text-on-surface-variant');
            }
            if (content) {
                content.classList.add('hidden');
            }
        }
    });
}

// Mega Menu Controller (Click toggle + Seamless Hover + Outside click handling)
let isMegaMenuPinned = false;

function toggleMegaMenu(e) {
    if (e) {
        e.preventDefault();
        e.stopPropagation();
    }
    const dropdown = document.getElementById('megaMenuDropdown');
    const trigger = document.getElementById('megaMenuTriggerBtn');
    if (!dropdown || !trigger) return;

    isMegaMenuPinned = !isMegaMenuPinned;

    if (isMegaMenuPinned) {
        openMegaMenu();
    } else {
        closeMegaMenu();
    }
}

function openMegaMenu() {
    const dropdown = document.getElementById('megaMenuDropdown');
    const trigger = document.getElementById('megaMenuTriggerBtn');
    if (!dropdown) return;
    dropdown.classList.remove('opacity-0', 'pointer-events-none', '-translate-y-1');
    dropdown.classList.add('opacity-100', 'pointer-events-auto', 'translate-y-0');
    if (trigger) {
        trigger.setAttribute('aria-expanded', 'true');
        trigger.classList.add('bg-emerald-50', 'border-emerald-300');
    }
}

function closeMegaMenu() {
    const dropdown = document.getElementById('megaMenuDropdown');
    const trigger = document.getElementById('megaMenuTriggerBtn');
    isMegaMenuPinned = false;
    if (!dropdown) return;
    dropdown.classList.remove('opacity-100', 'pointer-events-auto', 'translate-y-0');
    dropdown.classList.add('opacity-0', 'pointer-events-none', '-translate-y-1');
    if (trigger) {
        trigger.setAttribute('aria-expanded', 'false');
        trigger.classList.remove('bg-emerald-50', 'border-emerald-300');
    }
}

// Close mega menu when clicking anywhere outside or pressing Escape
document.addEventListener('click', (e) => {
    const wrapper = document.getElementById('megaMenuWrapper');
    if (wrapper && !wrapper.contains(e.target)) {
        closeMegaMenu();
    }
});

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeMegaMenu();
    }
});

// Mega Menu Category Hover Switcher
function showMegaSubCategory(slug) {
    const items = document.querySelectorAll('.mega-cat-item');
    items.forEach(el => {
        if (el.getAttribute('data-slug') === slug) {
            el.classList.add('active', 'bg-white', 'text-[#15803D]', 'shadow-xs', 'ring-1', 'ring-slate-100');
            el.classList.remove('text-slate-700');
        } else {
            el.classList.remove('active', 'bg-white', 'text-[#15803D]', 'shadow-xs', 'ring-1', 'ring-slate-100');
            el.classList.add('text-slate-700');
        }
    });

    const subPanels = document.querySelectorAll('.mega-subcontent');
    subPanels.forEach(panel => {
        panel.classList.add('hidden');
    });

    const targetPanel = document.getElementById(`mega-content-${slug}`);
    if (targetPanel) {
        targetPanel.classList.remove('hidden');
    }
}

// Mobile Drawer Category Accordion Toggle
function toggleMobileCategoryAccordion(slug) {
    const body = document.getElementById(`acc-body-${slug}`);
    const arrow = document.getElementById(`acc-arrow-${slug}`);
    if (body) {
        const isHidden = body.classList.contains('hidden');
        if (isHidden) {
            body.classList.remove('hidden');
            if (arrow) arrow.classList.add('rotate-180');
        } else {
            body.classList.add('hidden');
            if (arrow) arrow.classList.remove('rotate-180');
        }
    }
}



