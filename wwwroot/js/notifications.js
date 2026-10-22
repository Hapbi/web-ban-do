(function () {
    "use strict";

    function dismissToast(toast) {
        if (!toast || toast.classList.contains("is-leaving")) return;
        toast.classList.add("is-leaving");
        window.setTimeout(function () {
            if (toast.parentNode) toast.parentNode.removeChild(toast);
            var stack = document.getElementById("siteToastStack");
            if (stack && stack.children.length === 0) stack.remove();
        }, 240);
    }

    document.addEventListener("DOMContentLoaded", function () {
        var stack = document.getElementById("siteToastStack");
        if (!stack) return;

        Array.prototype.forEach.call(stack.querySelectorAll(".site-toast"), function (toast) {
            var close = toast.querySelector(".site-toast-close");
            if (close) {
                close.addEventListener("click", function () {
                    dismissToast(toast);
                });
            }
            window.setTimeout(function () {
                dismissToast(toast);
            }, 5500);
        });
    });
})();
