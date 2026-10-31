// Progress indicator for the Blazor WebAssembly boot. Kept in an external file
// so the Content-Security-Policy can forbid inline scripts.
(function () {
    let total = 0;
    let loaded = 0;
    const standardFetch = fetch;

    window.fetch = function (url, options) {
        total++;

        const promise = standardFetch(url, options);

        promise.then(function (data) {
            const element = document.getElementById('loadingText');

            if (element) {
                loaded++;

                const progress = Math.floor((loaded / total) * 100);
                const parsedUrl = new URL(data.url);

                element.textContent = progress + '% - ';

                const detail = document.createElement('i');
                detail.textContent = 'loaded ' + parsedUrl.pathname;
                element.appendChild(detail);
            }
        });

        return promise;
    };
})();
