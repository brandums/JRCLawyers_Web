import { Component, Input } from '@angular/core';
import { Usuario } from '../Models/Usuario';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
  @Input() user: Usuario | undefined;
  isExpanded = false;

  constructor(private authService: AuthService) { }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }

  logout() {
    this.authService.logout();
  }
}
