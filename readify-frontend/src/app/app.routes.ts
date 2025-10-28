import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'books', pathMatch: 'full' },
  { path: 'books', loadComponent: () => import('./pages/books/book-list.component').then(m => m.BookListComponent) },
  { path: 'books/:id', loadComponent: () => import('./pages/books/book-detail.component').then(m => m.BookDetailComponent) },
  { path: 'categories/:id', loadComponent: () => import('./pages/books/book-list.component').then(m => m.BookListComponent) },
  { path: 'cart', loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent), canActivate: [authGuard] },
  { path: 'orders', loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent), canActivate: [authGuard] },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'reset-password/:token', loadComponent: () => import('./pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
  { path: 'home', loadComponent: () => import('./pages/home/home-redirect.component').then(m => m.HomeRedirectComponent), canActivate: [authGuard] },
  { path: 'user/home', redirectTo: 'books', pathMatch: 'full' },
  { path: 'admin/home', redirectTo: 'admin/dashboard', pathMatch: 'full' },
  { path: 'profile', loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },

  // Admin area
  { path: 'admin/dashboard', loadComponent: () => import('./pages/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent), canActivate: [authGuard] },
  { path: 'admin/products', loadComponent: () => import('./pages/admin/product-list.component').then(m => m.AdminProductListComponent), canActivate: [authGuard] },
  { path: 'admin/products/new', loadComponent: () => import('./pages/admin/product-form.component').then(m => m.AdminProductFormComponent), canActivate: [authGuard] },
  { path: 'admin/products/:id', loadComponent: () => import('./pages/admin/product-form.component').then(m => m.AdminProductFormComponent), canActivate: [authGuard] },

  { path: '**', redirectTo: 'books' }
];
