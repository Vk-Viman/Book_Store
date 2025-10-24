import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-home-redirect',
  standalone: true,
  template: `<div></div>`
})
export class HomeRedirectComponent implements OnInit {
  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit() {
    // Redirect based on role directly to final destination
    if (this.auth.isAdmin()) {
      this.router.navigateByUrl('/admin/products');
    } else {
      this.router.navigateByUrl('/books');
    }
  }
}
