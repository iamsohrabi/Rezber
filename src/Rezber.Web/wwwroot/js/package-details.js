$(function () {
    
    //  Download chart (Canvas)
    const canvas = document.getElementById('downloadChart');
    if (!canvas) return;

    const dataAttr = canvas.getAttribute('data-values');
    if (!dataAttr) return;

    const data = dataAttr.split(',').map(x => parseInt(x, 10)).filter(x => !isNaN(x));
    if (data.length === 0) return;

    const dpr = window.devicePixelRatio || 1;

    function drawChart() {
        const rect = canvas.getBoundingClientRect();
        if (rect.width === 0) return;

        canvas.width = rect.width * dpr;
        canvas.height = rect.height * dpr;

        const ctx = canvas.getContext('2d');
        ctx.scale(dpr, dpr);

        const w = rect.width, h = rect.height;
        const padding = { top: 10, right: 4, bottom: 10, left: 4 };
        const chartW = w - padding.left - padding.right;
        const chartH = h - padding.top - padding.bottom;

        ctx.clearRect(0, 0, w, h);

        const max = Math.max(...data);
        const min = Math.min(...data);
        const range = max - min || 1;
        const stepX = chartW / (data.length - 1);

        const points = data.map((v, i) => ({
            x: padding.left + i * stepX,
            y: padding.top + chartH - ((v - min) / range) * chartH
        }));

        const grad = ctx.createLinearGradient(0, 0, 0, h);
        grad.addColorStop(0, 'rgba(255,255,255,0.15)');
        grad.addColorStop(1, 'rgba(255,255,255,0)');

        // Area
        ctx.beginPath();
        ctx.moveTo(points[0].x, h);
        points.forEach((p, i) => {
            if (i === 0) ctx.lineTo(p.x, p.y);
            else {
                const prev = points[i - 1];
                const cx = (prev.x + p.x) / 2;
                ctx.bezierCurveTo(cx, prev.y, cx, p.y, p.x, p.y);
            }
        });
        ctx.lineTo(points[points.length - 1].x, h);
        ctx.closePath();
        ctx.fillStyle = grad;
        ctx.fill();

        // Line
        ctx.beginPath();
        points.forEach((p, i) => {
            if (i === 0) ctx.moveTo(p.x, p.y);
            else {
                const prev = points[i - 1];
                const cx = (prev.x + p.x) / 2;
                ctx.bezierCurveTo(cx, prev.y, cx, p.y, p.x, p.y);
            }
        });
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 1.8;
        ctx.lineJoin = 'round';
        ctx.stroke();

        // Last point
        const last = points[points.length - 1];
        ctx.beginPath();
        ctx.arc(last.x, last.y, 4, 0, Math.PI * 2);
        ctx.fillStyle = '#ffffff';
        ctx.fill();

        // Labels
        const labelsHtml = [];
        const today = new Date();
        for (let i = data.length - 1; i >= 0; i -= 3) {
            const d = new Date(today);
            d.setDate(d.getDate() - i);
            labelsHtml.push('<span>' +
                d.toLocaleDateString('fa-IR', { month: '2-digit', day: '2-digit' }) +
                '</span>');
        }
        $('#chartLabels').html(labelsHtml.join(''));
    }

    // Draw when the stats tab becomes visible
    const statsTab = document.querySelector('[data-stats-tab]');
    if (statsTab) {
        statsTab.addEventListener('shown.bs.tab', () => {
            setTimeout(drawChart, 50);
        });
    }

    // Resize
    let resizeTimer;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(drawChart, 200);
    });

});
