window.initImageFallback = function () {
    const images = document.querySelectorAll('.cover-image');
    images.forEach(img => {
        img.onerror = function () {
            const fallbackSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <rect x="2" y="2" width="20" height="20" rx="2.18" ry="2.18"></rect>
                <line x1="7" y1="2" x2="7" y2="22"></line>
                <line x1="17" y1="2" x2="17" y2="22"></line>
                <line x1="2" y1="12" x2="22" y2="12"></line>
            </svg>`;

            const fallbackDiv = document.createElement('div');
            fallbackDiv.className = 'image-fallback';
            fallbackDiv.innerHTML = fallbackSvg;
            fallbackDiv.style.display = 'flex';
            fallbackDiv.style.alignItems = 'center';
            fallbackDiv.style.justifyContent = 'center';
            fallbackDiv.style.width = '100%';
            fallbackDiv.style.height = '100%';
            fallbackDiv.style.backgroundColor = 'var(--bg-tertiary, #2a2a2a)';

            const container = this.parentElement;
            if (container) {
                this.remove();
                container.appendChild(fallbackDiv);
            }
        };

        if (img.complete && (img.naturalWidth === 0 || img.naturalHeight === 0)) {
            img.onerror();
        }
    });
};

window.observeImageFallback = function () {
    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === 1 && node.tagName === 'IMG' && node.classList.contains('cover-image')) {
                    node.onerror = function () {
                        const fallbackSvg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                            <rect x="2" y="2" width="20" height="20" rx="2.18" ry="2.18"></rect>
                            <line x1="7" y1="2" x2="7" y2="22"></line>
                            <line x1="17" y1="2" x2="17" y2="22"></line>
                            <line x1="2" y1="12" x2="22" y2="12"></line>
                        </svg>`;

                        const fallbackDiv = document.createElement('div');
                        fallbackDiv.className = 'image-fallback';
                        fallbackDiv.innerHTML = fallbackSvg;
                        fallbackDiv.style.display = 'flex';
                        fallbackDiv.style.alignItems = 'center';
                        fallbackDiv.style.justifyContent = 'center';
                        fallbackDiv.style.width = '100%';
                        fallbackDiv.style.height = '100%';
                        fallbackDiv.style.backgroundColor = 'var(--bg-tertiary, #2a2a2a)';

                        const container = this.parentElement;
                        if (container) {
                            this.remove();
                            container.appendChild(fallbackDiv);
                        }
                    };

                    if (node.complete && (node.naturalWidth === 0 || node.naturalHeight === 0)) {
                        node.onerror();
                    }
                }
            });
        });
    });

    observer.observe(document.body, { childList: true, subtree: true });
};