import { Component } from '@angular/core';
import { Usuario } from '../Models/Usuario';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  user: Usuario | null = null;

  constructor(private authService: AuthService) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(user => {
      this.user = user;
    });
  }
}
