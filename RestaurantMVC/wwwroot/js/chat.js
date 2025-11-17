// Basic polling chat client for customer and admin
(function () {
  const api = {
    init: '/Chat/Init',
    fetch: '/Chat/Fetch',
    send: '/Chat/Send'
  };

  let conversationId = null;
  let polling = null;

  function el(id) { return document.getElementById(id); }

  function renderMessage(msg) {
    const wrapper = document.createElement('div');
    const isAdmin = (msg.sender || '').toLowerCase() === 'admin';
    wrapper.className = 'chat-msg ' + (isAdmin ? 'from-admin' : 'from-customer');
    const who = isAdmin ? 'Admin' : (msg.displayName || 'Bạn');
    const time = new Date(msg.createdAt).toLocaleTimeString();
    wrapper.innerHTML = `<div class="who">${who} • ${time}</div><div class="text">${escapeHtml(msg.text)}</div>`;
    return wrapper;
  }

  function escapeHtml(s) {
    const div = document.createElement('div');
    div.innerText = s;
    return div.innerHTML;
  }

  async function initConversation() {
    try {
      const r = await fetch(api.init);
      const j = await r.json();
      conversationId = j.conversationId;
    } catch { /* ignore */ }
  }

  async function fetchMessages() {
    try {
      const r = await fetch(api.fetch);
      const j = await r.json();
      if (!j || !j.messages) return;
      const box = el('chat-messages');
      box.innerHTML = '';
      j.messages.forEach(m => box.appendChild(renderMessage(m)));
      box.scrollTop = box.scrollHeight;
    } catch { /* ignore */ }
  }

  async function sendMessage(text) {
    const fd = new FormData();
    fd.append('text', text);
    try {
      await fetch(api.send, { method: 'POST', body: fd });
      await fetchMessages();
    } catch { /* ignore */ }
  }

  function bindUI() {
    const toggle = el('chat-toggle');
    const panel = document.querySelector('#chat-widget .chat-panel');
    const closeBtn = el('chat-close');
    const minBtn = el('chat-min');
    const sendBtn = el('chat-send');
    const input = el('chat-input');

    if (toggle) toggle.addEventListener('click', () => {
      panel.classList.toggle('open');
      if (panel.classList.contains('open')) {
        fetchMessages();
      }
    });
    if (closeBtn) closeBtn.addEventListener('click', () => panel.classList.remove('open'));
    if (minBtn) minBtn.addEventListener('click', () => panel.classList.remove('open'));
    if (sendBtn) sendBtn.addEventListener('click', () => {
      const text = input.value.trim();
      if (text) { sendMessage(text); input.value = ''; }
    });
    if (input) input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { sendBtn.click(); }
    });
  }

  function startPolling() {
    if (polling) clearInterval(polling);
    polling = setInterval(fetchMessages, 2500);
  }

  window.ChatWidget = {
    init: async function () {
      await initConversation();
      bindUI();
      startPolling();
    }
  };
})();