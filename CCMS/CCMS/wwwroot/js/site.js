document.addEventListener("DOMContentLoaded", () => {
    const toggleNavbarButton = document.getElementById("toggleNavbar");
    const sidebar = document.getElementById("sidebar");
    const closeSidebarButton = document.getElementById("closeSidebar");

    toggleNavbarButton?.addEventListener("click", () => {
        sidebar?.classList.toggle("-translate-x-full");
    });

    closeSidebarButton?.addEventListener("click", () => {
        sidebar?.classList.add("-translate-x-full");
    });
});
