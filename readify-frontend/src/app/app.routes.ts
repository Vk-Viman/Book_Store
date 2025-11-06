import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent) },
  { path: 'books', loadComponent: () => import('./pages/books/book-list.component').then(m => m.BookListComponent) },
  { path: 'books/:id', loadComponent: () => import('./pages/books/book-detail.component').then(m => m.BookDetailComponent) },
  { path: 'categories/:id', loadComponent: () => import('./pages/books/book-list.component').then(m => m.BookListComponent) },
  { path: 'cart', loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent), canActivate: [authGuard] },
  { path: 'orders', loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent), canActivate: [authGuard] },
  { path: 'orders/:id', loadComponent: () => import('./pages/orders/order-detail.component').then(m => m.OrderDetailComponent), canActivate: [authGuard] },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'reset-password/:token', loadComponent: () => import('./pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
  { path: 'user/home', redirectTo: 'books', pathMatch: 'full' },
  { path: 'admin/home', redirectTo: 'admin/dashboard', pathMatch: 'full' },
  { path: 'profile', loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },
  { path: 'checkout', loadComponent: () => import('./pages/checkout/checkout.component').then(m => m.CheckoutComponent), canActivate: [authGuard] },

  // Admin area
  { path: 'admin/dashboard', loadComponent: () => import('./pages/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent), canActivate: [adminGuard] },
  { path: 'admin/orders', loadComponent: () => import('./pages/admin/admin-orders.component').then(m => m.AdminOrdersComponent), canActivate: [adminGuard] },
  { path: 'admin/users', loadComponent: () => import('./pages/admin/admin-users.component').then(m => m.AdminUsersComponent), canActivate: [adminGuard] },
  { path: 'admin/products', loadComponent: () => import('./pages/admin/product-list.component').then(m => m.AdminProductListComponent), canActivate: [adminGuard] },
  { path: 'admin/products/new', loadComponent: () => import('./pages/admin/product-form.component').then(m => m.AdminProductFormComponent), canActivate: [adminGuard] },
  { path: 'admin/products/:id', loadComponent: () => import('./pages/admin/product-form.component').then(m => m.AdminProductFormComponent), canActivate: [adminGuard] },
  { path: 'admin/promos', loadComponent: () => import('./pages/admin/promo-list.component').then(m => m.AdminPromoListComponent), canActivate: [adminGuard] },
  { path: 'admin/promos/new', loadComponent: () => import('./pages/admin/promo-form.component').then(m => m.AdminPromoFormComponent), canActivate: [adminGuard] },
  { path: 'admin/promos/:id', loadComponent: () => import('./pages/admin/promo-edit.component').then(m => m.AdminPromoEditComponent), canActivate: [adminGuard] },
  { path: 'admin/shipping', loadComponent: () => import('./pages/admin/shipping-form.component').then(m => m.AdminShippingFormComponent), canActivate: [adminGuard] },

  { path: '**', redirectTo: 'home' }
];
