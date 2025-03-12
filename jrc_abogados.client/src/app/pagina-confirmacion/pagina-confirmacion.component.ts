import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-pagina-confirmacion',
  templateUrl: './pagina-confirmacion.component.html',
  styleUrls: ['./pagina-confirmacion.component.css']
})
export class PaginaConfirmacionComponent implements OnInit {
  message: string = '';

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    const status = this.route.snapshot.paramMap.get('status');
    this.message = status === '1'
      ? 'Gracias, su cita fue confirmada.'
      : 'Gracias, su cita fue cancelada.';
  }
}
