import { Injectable ,inject} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { getallsubscriptionresponse ,createSubscriptionResponse, updateSubscriptionResponse,getonesubscriptionresponse} from '../models/getallsubscriptionresponse.models';
@Injectable({
  providedIn: 'root',
})


export class SubscriptionServices {
  private http = inject(HttpClient);


private apiUrl = 'https://localhost:7222/api/subscription';

getSubscriptions(): Observable<getallsubscriptionresponse> {
    return this.http.get<getallsubscriptionresponse>(`${this.apiUrl}/Getall`);
  }

createSubscription(body: createSubscriptionResponse): Observable<void> {
    return this.http.post<void>(
        `${this.apiUrl}/Create`,
        body
    );
}

deleteSubscription(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Delete/${id}`);
  }


updateSubscription(id: string,body: createSubscriptionResponse): Observable<updateSubscriptionResponse> {
return this.http.put<updateSubscriptionResponse>(`${this.apiUrl}/Update/${id}`,body);
  }

  getbyid(subscriptionId: string): Observable<getonesubscriptionresponse> {
    return this.http.get<getonesubscriptionresponse>(`${this.apiUrl}/getsubscription/${subscriptionId}`);
  }
}





  
 

 
  
