import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NotificationService } from './services/notification.service';
import { AuthService } from './services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, RouterLink],
  templateUrl: './app.component.html'
})
export class AppComponent {
  notify = inject(NotificationService);
  private auth = inject(AuthService);
  private router = inject(Router);

  loggedIn$: Observable<boolean> = this.auth.isLoggedIn();
  isAdmin$: Observable<boolean> = this.auth.isAdmin$();

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
