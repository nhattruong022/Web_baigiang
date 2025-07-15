// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Handle active navigation menu
document.addEventListener('DOMContentLoaded', function () {
  // Get current URL path
  const currentPath = window.location.pathname;

  // Get all navigation links
  const navLinks = document.querySelectorAll('.nav-links a');

  // Remove active class from all menu items
  navLinks.forEach(link => {
    link.parentElement.classList.remove('active');
  });

  // Find and activate the current menu item
  navLinks.forEach(link => {
    const href = link.getAttribute('href');
    if (href && currentPath.includes(href.split('/').pop())) {
      link.parentElement.classList.add('active');
    }
  });

  // Special handling for exact matches
  navLinks.forEach(link => {
    const href = link.getAttribute('href');
    if (href === currentPath) {
      link.parentElement.classList.add('active');
    }
  });
});
