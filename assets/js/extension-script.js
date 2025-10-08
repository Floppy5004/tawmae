function toggleAccordion(id) {
    const content = document.getElementById(id);
    if (!content) return;
    const header = content.previousElementSibling;
    if (!header || !header.classList.contains('accordion-header')) return;
    const icon = header.querySelector('.accordion-icon');
    if (!icon) return;
    const isOpen = content.classList.contains('open');
    if (!isOpen) {
        content.classList.add('open');
        icon.setAttribute("data-icon", "ic:baseline-keyboard-arrow-down");
        const baseId = id.replace('-acc', '');
        history.pushState(null, null, `#${baseId}`);
        setTimeout(() => {
            const offset = 250;
            const elementPosition = header.getBoundingClientRect().top + window.scrollY;
            window.scrollTo({ top: elementPosition - offset, behavior: "smooth" });
        }, 100);
    } else {
        content.classList.remove('open');
        icon.setAttribute("data-icon", "ic:baseline-keyboard-arrow-right");
        history.pushState(null, null, window.location.pathname);
    }
}



// ====================================================================================================================
function copyToClipboard(id) {
    const copyText = document.getElementById(id);
    const button = copyText.nextElementSibling;
    navigator.clipboard.writeText(copyText.getAttribute("data-copy-text")).then(() => {
        button.innerHTML = '<span class="iconify" data-icon="fluent:checkmark-20-filled"></span> Copied';
        button.classList.add('copied');
        setTimeout(() => {
            button.innerHTML = '<span class="iconify" data-icon="material-symbols:content-copy-outline-sharp"></span> Copy';
            button.classList.remove('copied');
        }, 2500);
    });
}

// ====================================================================================================================
function copyURLToClipboard(url) {
    const button = document.querySelector('#browser-source-url-acc .copy-btn');
    navigator.clipboard.writeText(url).then(() => {
        button.innerHTML = '<span class="iconify" data-icon="fluent:checkmark-20-filled"></span> Copied';
        button.classList.add('copied');
        setTimeout(() => {
            button.innerHTML = '<span class="iconify" data-icon="material-symbols:content-copy-outline-sharp"></span> Copy';
            button.classList.remove('copied');
        }, 2500);
    });
}

// ====================================================================================================================
function loadImportString(file) {
    const importText = document.getElementById("import-string");
    fetch(file)
        .then(response => response.text())
        .then(data => {
            importText.textContent = data.substring(0, 200) + '...';
            importText.setAttribute("data-copy-text", data);
        })
        .catch(err => console.error("Error loading file:", err));
}

// ====================================================================================================================
document.addEventListener("DOMContentLoaded", function () {
    const hash = window.location.hash.substring(1);
    if (hash) {
        const accId = `${hash}-acc`;
        toggleAccordion(accId);
        setTimeout(() => {
            const header = document.getElementById(accId)?.previousElementSibling;
            if (header) {
                const offset = 250;
                const elementPosition = header.getBoundingClientRect().top + window.scrollY;
                window.scrollTo({ top: elementPosition - offset, behavior: "smooth" });
            }
        }, 200);
    }
    const currentPage = window.location.pathname;
    if (currentPage.includes("dynamic-timers")) {
        loadImportString("/action-imports/dynamic-timers.txt");
    } else if (currentPage.includes("rotator")) {
        loadImportString("/action-imports/rotator.txt");
    } else if (currentPage.includes("stream-receipt")) {
        loadImportString("/action-imports/stream-receipt.txt");
    }else if (currentPage.includes("spotify-and-sb")) {
        loadImportString("/action-imports/spotify-and-sb.txt");
    } else if (currentPage.includes("bluesky-and-sb")) {
        loadImportString("/action-imports/bluesky-and-sb.txt");
    } else if (currentPage.includes("user-birthdays")) {
        loadImportString("/action-imports/user-birthdays.txt");
    } else if (currentPage.includes("movie-and-tv-show-quiz")) {
        loadImportString("/action-imports/movie-and-tv-show-quiz.txt");
    } else if (currentPage.includes("temporary-vip")) {
        loadImportString("/action-imports/temporary-vip.txt");
    } else if (currentPage.includes("channel-point-auction")) {
        loadImportString("/action-imports/channel-point-auction.txt");
    } else if (currentPage.includes("command-check")) {
        loadImportString("/action-imports/command-check.txt");
    } else if (currentPage.includes("hugs-and-sb")) {
        loadImportString("/action-imports/hugs-and-sb.txt");
    } else if (currentPage.includes("live-trigger")) {
        loadImportString("/action-imports/live-trigger.txt");
    } else if (currentPage.includes("lurks-and-sb")) {
        loadImportString("/action-imports/lurks-and-sb.txt");
    } else if (currentPage.includes("mod-tools")) {
        loadImportString("/action-imports/mod-tools.txt");
    } else if (currentPage.includes("raffle")) {
        loadImportString("/action-imports/raffle.txt");
    } else if (currentPage.includes("random-source-position")) {
        loadImportString("/action-imports/random-source-position.txt");
    } else if (currentPage.includes("reward-discount")) {
        loadImportString("/action-imports/reward-discount.txt");
    } else if (currentPage.includes("time-trigger")) {
        loadImportString("/action-imports/time-trigger.txt");
    } else if (currentPage.includes("tts-queue")) {
        loadImportString("/action-imports/tts-queue.txt");
    } else if (currentPage.includes("user-inventory")) {
        loadImportString("/action-imports/user-inventory.txt");
    } else if (currentPage.includes("hot-potato")) {
        loadImportString("/action-imports/hot-potato.txt");
    } else if (currentPage.includes("quick-maths")) {
        loadImportString("/action-imports/quick-maths.txt");
    } else if (currentPage.includes("stardew-valley")) {
        loadImportString("/action-imports/stardew-valley.txt");
    }
    else if (currentPage.includes("random-source-position")) {
        loadImportString("/action-imports/random-source-position.txt");
    }
    else if (currentPage.includes("giphy-and-sb")) {
        loadImportString("/action-imports/giphy-and-sb.txt");
    }
    else if (currentPage.includes("twitch-points")) {
        loadImportString("/action-imports/twitch-points.txt");
    }
    else if (currentPage.includes("twitch-watchtime")) {
        loadImportString("/action-imports/twitch-watchtime.txt");
    }
    else if (currentPage.includes("twitch-goalbar")) {
        loadImportString("/action-imports/twitch-goalbar.txt");
    }
    
    
});

// ====================================================================================================================

document.addEventListener("DOMContentLoaded", function() {
  const shareElements = document.querySelectorAll('.share-element');
  
  shareElements.forEach(shareEl => {
    shareEl.addEventListener('click', async function() {
      const textToCopy = shareEl.getAttribute('data-clipboard-text');
      
      try {
        await navigator.clipboard.writeText(textToCopy);
        
        const icon = shareEl.querySelector('.share-icon');
        const textEl = shareEl.querySelector('.share-text');
        const originalIcon = icon.getAttribute('data-icon');
        const originalText = textEl.textContent;
        
        icon.setAttribute('data-icon', 'material-symbols:check-rounded');
        textEl.textContent = 'Copied';
        
        setTimeout(function() {
          icon.setAttribute('data-icon', originalIcon);
          textEl.textContent = originalText;
        }, 2500);
      } catch (err) {
        console.error('Fehler beim Kopieren: ', err);
      }
    });
  });
});


// ====================================================================================================================
document.addEventListener("DOMContentLoaded", () => {
  const grid = document.querySelector(".all-extensions .related-grid");
  if (!grid) return;

  function shuffleCardsInGrid() {
    const cards = Array.from(grid.querySelectorAll("a.related-card"));
    for (let i = cards.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [cards[i], cards[j]] = [cards[j], cards[i]];
    }
    cards.forEach(card => grid.appendChild(card));
  }

  function insertRowDividers() {
    grid.querySelectorAll(".row-divider").forEach(el => el.remove());
    const cards = Array.from(grid.querySelectorAll("a.related-card"));
    const cols = getComputedStyle(grid).gridTemplateColumns.split(" ").length;
    if (!cols || cols < 1) return;

    for (let i = cols; i < cards.length; i += cols) {
      const hr = document.createElement("hr");
      hr.className = "section-divider row-divider";
      grid.insertBefore(hr, cards[i]);
    }
  }

  const isHome = location.pathname === "/" || location.pathname.endsWith("/index.html");
  if (isHome) {
    shuffleCardsInGrid();
  }

  insertRowDividers();

  let resizeTO;
  window.addEventListener("resize", () => {
    clearTimeout(resizeTO);
    resizeTO = setTimeout(insertRowDividers, 120);
  });
});


