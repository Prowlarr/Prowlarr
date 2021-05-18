import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AppProfile from './AppProfile';
import EditAppProfileModalConnector from './EditAppProfileModalConnector';
import styles from './AppProfiles.css';

class AppProfiles extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isAppProfileModalOpen: false
    };
  }

  //
  // Listeners

  onCloneAppProfilePress = (id) => {
    this.props.onCloneAppProfilePress(id);
    this.setState({ isAppProfileModalOpen: true });
  }

  onEditAppProfilePress = () => {
    this.setState({ isAppProfileModalOpen: true });
  }

  onModalClose = () => {
    this.setState({ isAppProfileModalOpen: false });
  }

  //
  // Render

  render() {
    const {
      items,
      isDeleting,
      onConfirmDeleteAppProfile,
      onCloneAppProfilePress,
      ...otherProps
    } = this.props;

    return (
      <FieldSet legend={translate('AppProfiles')}>
        <PageSectionContent
          errorMessage={translate('UnableToLoadAppProfiles')}
          {...otherProps}c={true}
        >
          <div className={styles.appProfiles}>
            {
              items.map((item) => {
                return (
                  <AppProfile
                    key={item.id}
                    {...item}
                    isDeleting={isDeleting}
                    onConfirmDeleteAppProfile={onConfirmDeleteAppProfile}
                    onCloneAppProfilePress={this.onCloneAppProfilePress}
                  />
                );
              })
            }

            <Card
              className={styles.addAppProfile}
              onPress={this.onEditAppProfilePress}
            >
              <div className={styles.center}>
                <Icon
                  name={icons.ADD}
                  size={45}
                />
              </div>
            </Card>
          </div>

          <EditAppProfileModalConnector
            isOpen={this.state.isAppProfileModalOpen}
            onModalClose={this.onModalClose}
          />
        </PageSectionContent>
      </FieldSet>
    );
  }
}

AppProfiles.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteAppProfile: PropTypes.func.isRequired,
  onCloneAppProfilePress: PropTypes.func.isRequired
};

export default AppProfiles;
