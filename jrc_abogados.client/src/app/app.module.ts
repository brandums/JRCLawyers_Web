import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { RecaptchaModule, RecaptchaFormsModule } from 'ng-recaptcha';
import { NgxMaskDirective, NgxMaskPipe } from 'ngx-mask';

import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';


import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { CreateClientComponent } from './create-client/create-client.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { ClientListComponent } from './client-list/client-list.component';
import { UserListComponent } from './user-list/user-list.component';
import { CreateCaseComponent } from './create-case/create-case.component';
import { CaseListComponent } from './case-list/case-list.component';
import { CreateAppointmentComponent } from './create-appointment/create-appointment.component';
import { AppointmentListComponent } from './appointment-list/appointment-list.component';
import { CreateReminderComponent } from './create-reminder/create-reminder.component';
import { ReminderListComponent } from './reminder-list/reminder-list.component';
import { AuthInterceptor } from './services/AuthInterceptor';
import { authGuard } from './services/AuthGuard';
import { loginGuard } from './services/login-guard';
import { CreateExpedientComponent } from './create-expedient/create-expedient.component';
import { ExpedientListComponent } from './expedient-list/expedient-list.component';
import { DocumentListComponent } from './document-list/document-list.component';
import { CreateDocumentComponent } from './create-document/create-document.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ReportListComponent } from './report-list/report-list.component';
import { CreateReportComponent } from './create-report/create-report.component';
import { PaginaConfirmacionComponent } from './pagina-confirmacion/pagina-confirmacion.component';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    LoginComponent,
    CreateClientComponent,
    ClientListComponent,
    CreateUserComponent,
    UserListComponent,
    CreateCaseComponent,
    CaseListComponent,
    CreateAppointmentComponent,
    AppointmentListComponent,
    CreateReminderComponent,
    ReminderListComponent,
    CreateExpedientComponent,
    ExpedientListComponent,
    DocumentListComponent,
    CreateDocumentComponent,
    CreateReportComponent,
    ReportListComponent,
    PaginaConfirmacionComponent
  ],
  imports: [
    MatButtonModule,
    MatToolbarModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,

    RecaptchaModule,
    NgxMaskDirective,
    RecaptchaFormsModule,
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full', canActivate: [authGuard] },
      { path: 'login', component: LoginComponent, pathMatch: 'full', canActivate: [loginGuard] },
      { path: 'client-list', component: ClientListComponent, canActivate: [authGuard] },
      { path: 'user-list', component: UserListComponent, canActivate: [authGuard] },
      { path: 'case-list', component: CaseListComponent, canActivate: [authGuard] },
      { path: 'appointment-list', component: AppointmentListComponent, canActivate: [authGuard] },
      { path: 'reminder-list', component: ReminderListComponent, canActivate: [authGuard] },
      { path: 'expedient-list', component: ExpedientListComponent, canActivate: [authGuard] },
      { path: 'document-list', component: DocumentListComponent, canActivate: [authGuard] },
      { path: 'report-list', component: ReportListComponent, canActivate: [authGuard] },
      { path: 'pagina-confirmacion/:status', component: PaginaConfirmacionComponent, pathMatch: 'full' }
    ]),
    BrowserAnimationsModule,
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    NgxMaskDirective,
    NgxMaskPipe
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
