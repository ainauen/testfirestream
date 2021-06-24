import React, { Component } from 'react';
import { Helmet } from 'react-helmet';

export class GetVideo extends Component {
  static displayName = GetVideo.name;
    constructor(props) {
        super(props);
        this.state = { locations: [], loading: true, url : "www.testingstring.com" };
        //this.url = "www.teststring.com"
        this.componentDidMount = this.componentDidMount.bind(this);
    }

    componentDidMount() {
        this.populateLocations();

    }

    static renderLocationsTable(locations) {
        return (
            <div>
            <Helmet>
			    <link href="https://amp.azure.net/libs/amp/2.3.6/skins/amp-default/azuremediaplayer.min.css" rel="stylesheet"></link>
			    <script src="https://amp.azure.net/libs/amp/2.3.6/azuremediaplayer.min.js"></script>
		    </Helmet>
		    <h1>Video</h1>
		    {/*<link href="https://amp.azure.net/libs/amp/2.3.6/skins/amp-default/azuremediaplayer.min.css" rel="stylesheet">*/ }
            {/*<script src="https://amp.azure.net/libs/amp/2.3.6/azuremediaplayer.min.js"></script>*/ }
                {locations.map(location =>
                <video id="vid1" className="azuremediaplayer amp-default-skin" autoPlay controls width="640" height="400" data-setup='{"nativeControlsForTouch":false}'>
			    {/*<source src="https://test0fire0account-usea.streaming.media.azure.net/33f1fc6a-cd1c-4884-815a-3d1b601b134d/dcf1add4-a3f7-4046-bb95-db6bf66e807d.ism/manifest"*/}
                <source src= {location.url}
				    type="application/vnd.ms-sstr+xml" />
                    </video>
                )}
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>Date</th>
                        {/*<th>Temp. (C)</th>*/}
                        {/*<th>Temp. (F)</th>*/}
                        <th>Summary</th>
                    </tr>
                </thead>
                <tbody>
                    {locations.map(location =>
                        <tr key={location.date}>
                            <td>{location.date}</td>
                            {/*<td>{forecast.temperatureC}</td>*/}
                            {/*<td>{forecast.temperatureF}</td>*/}
                            <td>{location.summary}</td>
                        </tr>
                    )}
                </tbody>
                </table>
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : GetVideo.renderLocationsTable(this.state.locations);

        return (
            <div>
                <h1 id="tabelLabel" >Streaming Location</h1>
                <p>This component demonstrates fetching data from the server.</p>
                <p aria-live="polite">URL: <strong>{this.state.url}</strong></p>
                {/*<button className="btn btn-primary" onClick={this.componentDidMount}>mount</button>*/}
                {contents}
            </div>
        );
    }

    async populateLocations() {
        const response = await fetch('streaminglocator');
        const data = await response.json();
        this.setState({ locations: data, loading: false, url: "button pressed" });
    }

}
